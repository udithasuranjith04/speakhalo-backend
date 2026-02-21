using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpeakHalo.Application.DTOs;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;
using SpeakHalo.Domain.Interfaces.Repositories;

namespace SpeakHalo.Infrastructure.Services.Voice;

/// <summary>
/// Connects Twilio Media Streams directly to the Gemini Multimodal Live API for native audio-in/audio-out.
/// Single WebSocket to Gemini replaces the old 3-service chain (STT -> text chat -> TTS).
/// Provides sub-1s latency, human-like voice with proactive audio, affective dialog,
/// barge-in, filler words, typing sounds, and built-in function calling.
/// </summary>
public class GeminiLiveVoiceService : IGeminiVoiceService
{
    private readonly IConfiguration _configuration;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAppointmentService _appointmentService;
    private readonly ICustomerService _customerService;
    private readonly ICallerRecognitionService _callerRecognition;
    private readonly IUsageTrackingService _usageTracking;
    private readonly ITwilioService _twilioService;
    private readonly ISentimentAnalysisService _sentimentAnalysis;
    private readonly ICacheService _cacheService;
    private readonly ILogger<GeminiLiveVoiceService> _logger;

    private readonly string _apiKey;
    private readonly string _liveModel;
    private readonly string _defaultVoice;

    // Per-call state: set in HandleMediaStreamAsync, used by tool handlers
    // Safe because this service is Scoped and each WebSocket gets its own DI scope
    private string? _callerPhone;
    private string? _callSid;
    private CancellationTokenSource? _linkedCts;
    private StringBuilder? _transcriptBuilder;
    private RecognizedCaller? _recognizedCaller;
    private int _toolCallCount;
    private bool _callerRecognitionEnabled;
    private AgentProfile? _agentProfile;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public GeminiLiveVoiceService(
        IConfiguration configuration,
        IUnitOfWork unitOfWork,
        IAppointmentService appointmentService,
        ICustomerService customerService,
        ICallerRecognitionService callerRecognition,
        IUsageTrackingService usageTracking,
        ITwilioService twilioService,
        ISentimentAnalysisService sentimentAnalysis,
        ICacheService cacheService,
        ILogger<GeminiLiveVoiceService> logger)
    {
        _configuration = configuration;
        _unitOfWork = unitOfWork;
        _appointmentService = appointmentService;
        _customerService = customerService;
        _callerRecognition = callerRecognition;
        _usageTracking = usageTracking;
        _twilioService = twilioService;
        _sentimentAnalysis = sentimentAnalysis;
        _cacheService = cacheService;
        _logger = logger;
        _apiKey = configuration["Gemini:ApiKey"]
            ?? throw new ArgumentNullException("Gemini:ApiKey", "Gemini API key is not configured");
        _liveModel = configuration["Gemini:LiveModel"] ?? "gemini-2.5-flash-native-audio-preview-12-2025";
        _defaultVoice = configuration["Gemini:DefaultVoice"] ?? "Kore";
    }

    public async Task HandleMediaStreamAsync(
        WebSocket twilioWs,
        string callSid,
        string agentId,
        string? callerPhone,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Gemini Live voice stream for call {CallSid}, agent {AgentId}", callSid, agentId);

        string? streamSid = null;
        var transcriptBuilder = new StringBuilder();

        // Store per-call state for use in tool handlers
        _callerPhone = callerPhone;
        _callSid = callSid;
        _transcriptBuilder = transcriptBuilder;

        // Load voice agent config
        var agent = await _unitOfWork.Repository<VoiceAgent>().GetByIdAsync(agentId, cancellationToken);
        string voiceName = agent?.VoiceName ?? _defaultVoice;
        string agentUserId = agent?.UserId ?? "";

        string systemPrompt = agent != null
            ? await BuildDynamicSystemPromptAsync(agent, callerPhone, cancellationToken)
            : BuildFallbackSystemPrompt();

        // Connect to Gemini Live API WebSocket
        using var geminiWs = new ClientWebSocket();
        var geminiUri = new Uri(
            $"wss://generativelanguage.googleapis.com/ws/google.ai.generativelanguage.v1beta.GenerativeService.BidiGenerateContent?key={_apiKey}");

        try
        {
            await geminiWs.ConnectAsync(geminiUri, cancellationToken);
            _logger.LogInformation("Connected to Gemini Live API for call {CallSid}", callSid);

            // Send setup message with proactiveAudio + affectiveDialog enabled
            await SendSetupMessageAsync(geminiWs, voiceName, systemPrompt, cancellationToken);

            // Wait for setupComplete
            if (!await WaitForSetupCompleteAsync(geminiWs, cancellationToken))
            {
                _logger.LogError("Gemini Live setup failed for call {CallSid}", callSid);
                return;
            }

            _logger.LogInformation("Gemini Live setup complete for call {CallSid}", callSid);

            // Run two parallel loops: Twilio->Gemini and Gemini->Twilio
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _linkedCts = linkedCts;

            // Enforce MaxCallDurationMinutes to prevent runaway calls
            var maxMinutes = agent?.MaxCallDurationMinutes ?? 30;
            linkedCts.CancelAfter(TimeSpan.FromMinutes(maxMinutes));
            _logger.LogInformation("Call {CallSid} will auto-terminate after {MaxMinutes} minutes", callSid, maxMinutes);

            var ct = linkedCts.Token;

            var twilioToGemini = TwilioToGeminiLoopAsync(twilioWs, geminiWs, s => streamSid = s, ct);
            var geminiToTwilio = GeminiToTwilioLoopAsync(
                geminiWs, twilioWs, () => streamSid, agentUserId, transcriptBuilder, linkedCts, ct);

            // Wait for either loop to end
            await Task.WhenAny(twilioToGemini, geminiToTwilio);
            linkedCts.Cancel();

            // Let the other loop finish cleanly
            try { await Task.WhenAll(twilioToGemini, geminiToTwilio); }
            catch (OperationCanceledException) { }
        }
        catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
        {
            _logger.LogInformation("WebSocket closed prematurely for call {CallSid}", callSid);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Voice stream cancelled for call {CallSid}", callSid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Gemini Live voice stream for call {CallSid}", callSid);
        }
        finally
        {
            _logger.LogInformation("Gemini Live voice stream ended for call {CallSid}", callSid);

            // Persist transcript and track AI minutes usage
            await PersistCallDataAsync(callSid, transcriptBuilder, agentUserId);

            // Close Gemini WS with a timeout to avoid hanging
            if (geminiWs.State == WebSocketState.Open)
            {
                try
                {
                    using var closeCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await geminiWs.CloseAsync(WebSocketCloseStatus.NormalClosure, "Call ended", closeCts.Token);
                }
                catch (Exception ex) { _logger.LogDebug(ex, "Best-effort Gemini WebSocket close"); }
            }

            // Close Twilio WS with a timeout to avoid hanging
            if (twilioWs.State == WebSocketState.Open)
            {
                try
                {
                    using var closeCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await twilioWs.CloseAsync(WebSocketCloseStatus.NormalClosure, "Call ended", closeCts.Token);
                }
                catch (Exception ex) { _logger.LogDebug(ex, "Best-effort Twilio WebSocket close"); }
            }
        }
    }

    #region Setup

    private async Task SendSetupMessageAsync(
        ClientWebSocket geminiWs, string voiceName, string systemPrompt, CancellationToken ct)
    {
        var setup = new
        {
            setup = new
            {
                model = $"models/{_liveModel}",
                generationConfig = new
                {
                    responseModalities = new[] { "AUDIO" },
                    speechConfig = new
                    {
                        voiceConfig = new
                        {
                            prebuiltVoiceConfig = new
                            {
                                voiceName
                            }
                        }
                    },
                    // Enable proactive audio: model makes "mm-hmm", breathing, and listening sounds
                    // while the caller is speaking — like a real human on the phone
                    proactiveAudio = true,
                    // Enable affective dialog: model reads caller's tone/emotion and adapts
                    // its voice to match (sympathetic for frustrated caller, upbeat for happy caller)
                    enableAffectiveDialog = true
                },
                systemInstruction = new
                {
                    parts = new[]
                    {
                        new { text = systemPrompt }
                    }
                },
                tools = BuildToolDeclarations(_callerRecognitionEnabled),
                realtimeInputConfig = new
                {
                    automaticActivityDetection = new
                    {
                        disabled = false
                    }
                },
                inputAudioTranscription = new { },
                outputAudioTranscription = new { }
            }
        };

        await SendJsonAsync(geminiWs, setup, ct);
    }

    private async Task<bool> WaitForSetupCompleteAsync(ClientWebSocket geminiWs, CancellationToken ct)
    {
        var buffer = new byte[8192];
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(10));

        try
        {
            var (json, _) = await ReceiveJsonAsync(geminiWs, buffer, timeoutCts.Token);
            if (json == null) return false;

            using (json)
                return json.RootElement.TryGetProperty("setupComplete", out _);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Timed out waiting for Gemini setupComplete");
            return false;
        }
    }

    #endregion

    #region Twilio -> Gemini Loop

    private async Task TwilioToGeminiLoopAsync(
        WebSocket twilioWs,
        ClientWebSocket geminiWs,
        Action<string> setStreamSid,
        CancellationToken ct)
    {
        var buffer = new byte[8192];

        try
        {
            while (twilioWs.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var result = await twilioWs.ReceiveAsync(new ArraySegment<byte>(buffer), ct);

                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                if (result.MessageType != WebSocketMessageType.Text)
                    continue;

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var twilioMsg = JsonSerializer.Deserialize<TwilioMediaMessage>(message);
                if (twilioMsg == null) continue;

                switch (twilioMsg.Event)
                {
                    case "connected":
                        _logger.LogDebug("Twilio Media Stream connected");
                        break;

                    case "start":
                        var sid = twilioMsg.Start?.StreamSid;
                        if (sid != null) setStreamSid(sid);
                        _logger.LogInformation("Twilio stream started: {StreamSid}", sid);
                        break;

                    case "media":
                        if (twilioMsg.Media?.Payload != null)
                        {
                            var mulawBytes = Convert.FromBase64String(twilioMsg.Media.Payload);
                            var pcm16kBytes = AudioConverter.TwilioToGemini(mulawBytes);
                            var pcmBase64 = Convert.ToBase64String(pcm16kBytes);

                            var audioInput = new
                            {
                                realtimeInput = new
                                {
                                    mediaChunks = new[]
                                    {
                                        new
                                        {
                                            mimeType = "audio/pcm;rate=16000",
                                            data = pcmBase64
                                        }
                                    }
                                }
                            };

                            await SendJsonAsync(geminiWs, audioInput, ct);
                        }
                        break;

                    case "stop":
                        _logger.LogInformation("Twilio stream stopped");
                        return;
                }
            }
        }
        catch (OperationCanceledException) { _logger.LogDebug("TwilioToGemini loop cancelled"); }
        catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
        {
            _logger.LogDebug("TwilioToGemini WebSocket closed prematurely");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in TwilioToGemini loop");
        }
    }

    #endregion

    #region Gemini -> Twilio Loop

    private async Task GeminiToTwilioLoopAsync(
        ClientWebSocket geminiWs,
        WebSocket twilioWs,
        Func<string?> getStreamSid,
        string agentUserId,
        StringBuilder transcriptBuilder,
        CancellationTokenSource linkedCts,
        CancellationToken ct)
    {
        var buffer = new byte[65536]; // Larger buffer for audio data

        try
        {
            while (geminiWs.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var (json, raw) = await ReceiveJsonAsync(geminiWs, buffer, ct);
                if (json == null) break;

                using (json)
                {
                    var root = json.RootElement;

                    // setupComplete (already handled, but may arrive again)
                    if (root.TryGetProperty("setupComplete", out _))
                        continue;

                    // serverContent — audio output, transcriptions, interruptions
                    if (root.TryGetProperty("serverContent", out var serverContent))
                    {
                        await HandleServerContentAsync(
                            serverContent, twilioWs, getStreamSid, transcriptBuilder, ct);
                        continue;
                    }

                    // toolCall — function calling
                    if (root.TryGetProperty("toolCall", out var toolCall))
                    {
                        // Send keyboard typing sounds to Twilio WHILE executing the tool
                        // This fills the silence so the caller hears realistic office sounds
                        var typingSoundTask = SendTypingSoundsAsync(twilioWs, getStreamSid, ct);
                        var toolCallTask = HandleToolCallAsync(toolCall, geminiWs, agentUserId, ct);
                        await Task.WhenAll(typingSoundTask, toolCallTask);
                        continue;
                    }

                    // toolCallCancellation — cancelled tool calls
                    if (root.TryGetProperty("toolCallCancellation", out var toolCallCancellation))
                    {
                        _logger.LogDebug("Tool call cancelled: {Ids}",
                            toolCallCancellation.TryGetProperty("ids", out var ids) ? ids.ToString() : "unknown");
                        continue;
                    }

                    // goAway — server asking to disconnect
                    if (root.TryGetProperty("goAway", out _))
                    {
                        _logger.LogWarning("Gemini Live sent goAway, closing gracefully");
                        linkedCts.Cancel();
                        return;
                    }
                }
            }
        }
        catch (OperationCanceledException) { _logger.LogDebug("GeminiToTwilio loop cancelled"); }
        catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
        {
            _logger.LogDebug("GeminiToTwilio WebSocket closed prematurely");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GeminiToTwilio loop");
        }
    }

    private async Task HandleServerContentAsync(
        JsonElement serverContent,
        WebSocket twilioWs,
        Func<string?> getStreamSid,
        StringBuilder transcriptBuilder,
        CancellationToken ct)
    {
        // Check for interruption — caller barged in
        if (serverContent.TryGetProperty("interrupted", out var interrupted) && interrupted.GetBoolean())
        {
            var streamSid = getStreamSid();
            if (streamSid != null)
            {
                var clearMsg = JsonSerializer.Serialize(new
                {
                    @event = "clear",
                    streamSid
                });
                await SendTextAsync(twilioWs, clearMsg, ct);
                _logger.LogDebug("Sent clear to Twilio (barge-in)");
            }
        }

        // Process audio output from modelTurn
        if (serverContent.TryGetProperty("modelTurn", out var modelTurn) &&
            modelTurn.TryGetProperty("parts", out var parts))
        {
            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("inlineData", out var inlineData) &&
                    inlineData.TryGetProperty("data", out var audioData))
                {
                    var pcm24kBytes = Convert.FromBase64String(audioData.GetString()!);
                    var mulawBytes = AudioConverter.GeminiToTwilio(pcm24kBytes);
                    var payload = Convert.ToBase64String(mulawBytes);

                    var streamSid = getStreamSid();
                    if (streamSid != null)
                    {
                        var mediaMsg = JsonSerializer.Serialize(new
                        {
                            @event = "media",
                            streamSid,
                            media = new { payload }
                        });
                        await SendTextAsync(twilioWs, mediaMsg, ct);
                    }
                }
            }
        }

        // Log input transcription (what user said)
        if (serverContent.TryGetProperty("inputTranscription", out var inputTranscription) &&
            inputTranscription.TryGetProperty("text", out var userText))
        {
            var text = userText.GetString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                _logger.LogInformation("Caller said: {Text}", text);
                transcriptBuilder.AppendLine($"Caller: {text}");
            }
        }

        // Log output transcription (what AI said)
        if (serverContent.TryGetProperty("outputTranscription", out var outputTranscription) &&
            outputTranscription.TryGetProperty("text", out var aiText))
        {
            var text = aiText.GetString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                _logger.LogInformation("AI said: {Text}", text);
                transcriptBuilder.AppendLine($"Agent: {text}");
            }
        }
    }

    #endregion

    #region Typing Sounds (Office Realism)

    /// <summary>
    /// Sends realistic keyboard typing sounds to Twilio while a tool call is executing.
    /// This fills the silence during database lookups / appointment booking so the caller
    /// hears what sounds like a real receptionist typing on a keyboard.
    /// Generated as mulaw 8kHz samples: soft key-click bursts with random spacing.
    /// </summary>
    private async Task SendTypingSoundsAsync(
        WebSocket twilioWs, Func<string?> getStreamSid, CancellationToken ct)
    {
        var streamSid = getStreamSid();
        if (streamSid == null) return;

        try
        {
            // Send 3-4 bursts of typing sounds with natural random spacing
            // Each burst = 150-300ms of clicks, gap = 100-400ms of near-silence
            var rng = Random.Shared;
            int bursts = rng.Next(3, 5);

            for (int i = 0; i < bursts && !ct.IsCancellationRequested; i++)
            {
                // Generate a typing burst
                int burstMs = rng.Next(150, 300);
                int burstSamples = burstMs * 8; // 8 samples per ms at 8kHz
                var burstAudio = GenerateTypingBurst(burstSamples, rng);

                var payload = Convert.ToBase64String(burstAudio);
                var mediaMsg = JsonSerializer.Serialize(new
                {
                    @event = "media",
                    streamSid,
                    media = new { payload }
                });
                await SendTextAsync(twilioWs, mediaMsg, ct);

                // Natural gap between bursts (like pausing between words while typing)
                int gapMs = rng.Next(100, 400);
                await Task.Delay(gapMs, ct);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Typing sound generation interrupted");
        }
    }

    /// <summary>
    /// Generates mulaw-encoded audio that sounds like soft keyboard typing.
    /// Creates a series of short click impulses (2-4ms each) at random intervals
    /// within the burst, mixed with very low ambient office hum.
    /// </summary>
    private static byte[] GenerateTypingBurst(int sampleCount, Random rng)
    {
        var mulaw = new byte[sampleCount];

        // Fill with near-silence (mulaw 0xFF = ~0 amplitude)
        // Use 0xFE/0xFF alternation for subtle ambient hum
        for (int i = 0; i < sampleCount; i++)
            mulaw[i] = (byte)(i % 3 == 0 ? 0xFE : 0xFF);

        // Place 4-8 individual key clicks within the burst
        int clickCount = rng.Next(4, 9);
        for (int c = 0; c < clickCount; c++)
        {
            int clickStart = rng.Next(0, Math.Max(1, sampleCount - 24));
            int clickLen = rng.Next(16, 25); // 2-3ms click at 8kHz

            for (int j = 0; j < clickLen && (clickStart + j) < sampleCount; j++)
            {
                // Shape: sharp attack (loud), quick exponential decay
                // Mulaw: lower values = louder. Vary intensity per click for realism.
                double progress = (double)j / clickLen;
                double envelope = Math.Exp(-progress * 4.0); // Fast decay
                int loudness = (int)(envelope * rng.Next(20, 50)); // Soft clicks, not jarring

                // Alternate polarity for click texture
                byte mulawSample = (byte)(j % 2 == 0
                    ? Math.Max(0x80, 0xFF - loudness)   // Positive half
                    : Math.Min(0x7F, 0x00 + loudness)); // Negative half

                mulaw[clickStart + j] = mulawSample;
            }
        }

        return mulaw;
    }

    #endregion

    #region Function Calling

    private async Task HandleToolCallAsync(
        JsonElement toolCall,
        ClientWebSocket geminiWs,
        string agentUserId,
        CancellationToken ct)
    {
        if (!toolCall.TryGetProperty("functionCalls", out var functionCalls))
            return;

        // Rate limit tool calls to prevent runaway AI loops
        const int maxToolCallsPerSession = 30;
        var responses = new List<object>();

        foreach (var fc in functionCalls.EnumerateArray())
        {
            var name = fc.GetProperty("name").GetString()!;
            var id = fc.GetProperty("id").GetString()!;
            var args = fc.TryGetProperty("args", out var argsEl) ? argsEl : default;

            _logger.LogInformation("Tool call: {Name} (id: {Id})", name, id);

            if (++_toolCallCount > maxToolCallsPerSession)
            {
                _logger.LogWarning("Tool call limit ({Limit}) exceeded, rejecting {Name}", maxToolCallsPerSession, name);
                responses.Add(new { name, id, response = new { error = "Too many actions in this call. Please try again." } });
                continue;
            }

            object result;
            try
            {
                result = name switch
                {
                    "check_availability" => await HandleCheckAvailabilityAsync(args, agentUserId, ct),
                    "book_appointment" => await HandleBookAppointmentAsync(args, agentUserId, ct),
                    "get_business_info" => await HandleGetBusinessInfoAsync(args, agentUserId, ct),
                    "save_customer" => await HandleSaveCustomerAsync(args, agentUserId, ct),
                    "update_customer" => await HandleUpdateCustomerAsync(args, agentUserId, ct),
                    "transfer_to_human" => await HandleTransferToHumanAsync(args, agentUserId, ct),
                    "end_call" => await HandleEndCallAsync(args, agentUserId, ct),
                    "reschedule_appointment" => await HandleRescheduleAppointmentAsync(args, agentUserId, ct),
                    "cancel_appointment" => await HandleCancelAppointmentAsync(args, agentUserId, ct),
                    _ => new { error = $"Unknown function: {name}" }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing tool {Name}", name);
                result = new { error = $"Failed to execute {name}. Please try again." };
            }

            responses.Add(new
            {
                name,
                id,
                response = result
            });
        }

        var toolResponse = new
        {
            toolResponse = new
            {
                functionResponses = responses
            }
        };

        await SendJsonAsync(geminiWs, toolResponse, ct);
    }

    private async Task<object> HandleCheckAvailabilityAsync(JsonElement args, string userId, CancellationToken ct)
    {
        var dateStr = args.TryGetProperty("date", out var d) ? d.GetString() : null;
        var serviceType = args.TryGetProperty("service_type", out var s) ? s.GetString() : null;

        if (string.IsNullOrEmpty(userId))
            return new { available = true, message = "Availability checking requires a configured business account." };

        var targetDate = ParseRelativeDate(dateStr);
        var existingAppointments = await _unitOfWork.Repository<Appointment>()
            .FindAsync(a => a.UserId == userId
                && a.ScheduledAt.Date == targetDate.Date
                && a.Status != AppointmentStatus.Cancelled
                && !a.IsDeleted, ct);

        var bookedSlots = existingAppointments
            .OrderBy(a => a.ScheduledAt)
            .Select(a => $"{a.ScheduledAt:h:mm tt} - {a.ScheduledAt.AddMinutes(a.DurationMinutes):h:mm tt}")
            .ToList();

        return new
        {
            available = true,
            date = targetDate.ToString("dddd, MMMM d"),
            bookedSlots = bookedSlots.Count > 0 ? bookedSlots : null as List<string>,
            message = bookedSlots.Count > 0
                ? $"We have {bookedSlots.Count} appointment(s) on {targetDate:dddd, MMMM d}. Let me find a time that works."
                : $"{targetDate:dddd, MMMM d} is wide open! What time works best for you?"
        };
    }

    private async Task<object> HandleBookAppointmentAsync(JsonElement args, string userId, CancellationToken ct)
    {
        var customerName = args.TryGetProperty("customer_name", out var cn) ? cn.GetString() : null;
        var customerPhone = args.TryGetProperty("customer_phone", out var cp) ? cp.GetString() : null;
        var dateTimeStr = args.TryGetProperty("date_time", out var dt) ? dt.GetString() : null;
        var serviceTypeStr = args.TryGetProperty("service_type", out var st) ? st.GetString() : null;
        var description = args.TryGetProperty("description", out var desc) ? desc.GetString() : null;
        var customerAddress = args.TryGetProperty("customer_address", out var ca) ? ca.GetString() : null;

        if (string.IsNullOrEmpty(userId))
            return new { success = false, message = "Appointment booking requires a configured business account." };

        var scheduledAt = ParseRelativeDate(dateTimeStr);
        var serviceType = ParseServiceType(serviceTypeStr);
        var customerId = _recognizedCaller?.CustomerId;

        // If no customer ID from recognition, try looking up by phone
        if (string.IsNullOrEmpty(customerId) && !string.IsNullOrEmpty(customerPhone))
        {
            var customers = await _unitOfWork.Repository<Customer>()
                .FindAsync(c => c.UserId == userId && c.Phone == customerPhone && !c.IsDeleted, ct);
            customerId = customers.FirstOrDefault()?.Id;
        }

        // Customer is required for booking — tell AI to save customer first
        if (string.IsNullOrEmpty(customerId))
        {
            return new
            {
                success = false,
                message = "I need to save the customer's details first before I can book. Let me do that real quick."
            };
        }

        var createRequest = new CreateAppointmentRequest
        {
            UserId = userId,
            CustomerId = customerId,
            ScheduledAt = scheduledAt,
            DurationMinutes = 60,
            ServiceType = serviceType?.ToString() ?? "Cleaning",
            Location = customerAddress,
            Notes = description
        };

        var bookResult = await _appointmentService.CreateAppointmentAsync(createRequest, ct);

        if (bookResult.IsSuccess)
        {
            // Run post-booking tasks in parallel — don't block the voice response
            var postBookingTasks = new List<Task>();

            // Mark call as having booked an appointment (for conversion tracking)
            if (!string.IsNullOrEmpty(_callSid))
            {
                postBookingTasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var calls = await _unitOfWork.Repository<Call>()
                            .FindAsync(c => c.TwilioCallSid == _callSid && !c.IsDeleted, ct);
                        var call = calls.FirstOrDefault();
                        if (call != null)
                        {
                            call.AppointmentScheduled = true;
                            _unitOfWork.Repository<Call>().Update(call);
                            await _unitOfWork.SaveChangesAsync(ct);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to mark call {CallSid} as appointment scheduled", _callSid);
                    }
                }, ct));
            }

            // Send SMS confirmation to the customer
            if (!string.IsNullOrEmpty(customerPhone) && !string.IsNullOrEmpty(userId))
            {
                postBookingTasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var businessPhone = await GetBusinessPhoneAsync(userId, ct);
                        if (!string.IsNullOrEmpty(businessPhone))
                        {
                            var profile = (await _unitOfWork.Repository<UserProfile>()
                                .FindAsync(p => p.UserId == userId, ct)).FirstOrDefault();
                            var businessName = profile?.BusinessName ?? "us";

                            var bookedAppt = bookResult.Data!;
                            var smsMessage = $"Hi {customerName}, your appointment with {businessName} is confirmed for {bookedAppt.ScheduledAt:dddd, MMMM d} at {bookedAppt.ScheduledAt:h:mm tt}. " +
                                $"If you need to reschedule or cancel, please call us back. Thank you!";

                            await _twilioService.SendSmsAsync(customerPhone, businessPhone, smsMessage, ct);
                            await _usageTracking.TrackSMSMessageAsync(userId);
                            _logger.LogInformation("Sent SMS booking confirmation to {Phone} for appointment {AppointmentId}", customerPhone, bookedAppt.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send SMS booking confirmation to {Phone}", customerPhone);
                    }
                }, ct));
            }

            // Await all post-booking tasks without blocking voice response
            // (errors are caught inside each task)
            _ = Task.WhenAll(postBookingTasks).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    _logger.LogWarning(t.Exception, "Post-booking task(s) failed for call {CallSid}", _callSid);
            }, TaskScheduler.Default);

            var appt = bookResult.Data!;
            return new
            {
                success = true,
                appointmentId = appt.Id,
                scheduledAt = appt.ScheduledAt.ToString("dddd, MMMM d 'at' h:mm tt"),
                message = $"All set! Appointment booked for {appt.ScheduledAt:dddd, MMMM d} at {appt.ScheduledAt:h:mm tt}."
            };
        }

        return new
        {
            success = false,
            message = string.Join("; ", bookResult.Errors)
        };
    }

    private async Task<string?> GetBusinessPhoneAsync(string userId, CancellationToken ct)
    {
        var phoneNumber = await _unitOfWork.Repository<PhoneNumber>()
            .FirstOrDefaultAsync(p => p.UserId == userId && !p.IsDeleted, ct);
        return phoneNumber?.Number;
    }

    private async Task<object> HandleGetBusinessInfoAsync(JsonElement args, string userId, CancellationToken ct)
    {
        var infoType = args.TryGetProperty("info_type", out var it) ? it.GetString()?.ToLowerInvariant() : "general";

        if (string.IsNullOrEmpty(userId))
            return new { info = "Business information is not available for this agent." };

        var profiles = await _unitOfWork.Repository<UserProfile>()
            .FindAsync(p => p.UserId == userId, ct);
        var profile = profiles.FirstOrDefault();

        var businessName = profile?.BusinessName ?? "our company";
        var industry = profile?.Industry.ToString() ?? "cleaning services";

        return infoType switch
        {
            "hours" => new { info = $"{businessName} is available Monday through Friday, 8 AM to 6 PM, and Saturdays 9 AM to 2 PM." } as object,
            "services" => new { info = $"{businessName} provides professional {industry} services including residential cleaning, deep cleaning, move-in/move-out, and commercial cleaning." },
            "location" => new { info = $"We serve the local area. I can take down your address and we'll confirm we can service your location." },
            "pricing" => new { info = $"Pricing depends on the specific service needed. I can have our team provide a detailed quote. Would you like to schedule an estimate?" },
            _ => new { info = $"{businessName} is a professional {industry} company. How can I help you today?" }
        };
    }

    private async Task<object> HandleSaveCustomerAsync(JsonElement args, string userId, CancellationToken ct)
    {
        // Guard: respect business opt-out of caller recognition
        if (!_callerRecognitionEnabled)
            return new { success = false, message = "Customer management is not enabled for this account." };

        var customerName = args.TryGetProperty("customer_name", out var cn) ? cn.GetString() : null;
        var customerPhone = args.TryGetProperty("customer_phone", out var cp) ? cp.GetString() : _callerPhone;
        var customerEmail = args.TryGetProperty("customer_email", out var ce) ? ce.GetString() : null;
        var customerAddress = args.TryGetProperty("customer_address", out var ca) ? ca.GetString() : null;
        var notes = args.TryGetProperty("notes", out var n) ? n.GetString() : null;

        // Prefer actual caller phone over AI-supplied value to prevent hallucinated phone associations
        if (!string.IsNullOrEmpty(_callerPhone) && customerPhone != _callerPhone)
        {
            _logger.LogWarning("AI supplied different phone than caller, using actual caller phone");
            customerPhone = _callerPhone;
        }

        if (string.IsNullOrEmpty(userId))
            return new { success = false, message = "Customer saving requires a configured business account." };

        if (string.IsNullOrEmpty(customerName) || string.IsNullOrEmpty(customerPhone))
            return new { success = false, message = "Need at least a name and phone number to save the customer." };

        // Check if customer already exists for this business
        var existing = await _unitOfWork.Repository<Customer>()
            .FindAsync(c => c.UserId == userId && c.Phone == customerPhone && !c.IsDeleted, ct);
        if (existing.Any())
            return new { success = true, message = $"Customer {customerName} is already in the system." };

        var request = new CreateCustomerRequest
        {
            UserId = userId,
            Phone = customerPhone,
            Name = customerName,
            Email = customerEmail,
            Address = customerAddress,
            Notes = notes ?? "Added via voice call"
        };

        var result = await _customerService.CreateCustomerAsync(request, ct);

        if (result.IsSuccess && result.Data != null)
        {
            // Update the in-memory recognized caller so book_appointment uses the real CustomerId
            _recognizedCaller = _recognizedCaller != null
                ? _recognizedCaller with { CustomerId = result.Data.Id, Name = customerName }
                : new RecognizedCaller(
                    CustomerId: result.Data.Id, Name: customerName, JobCount: 0,
                    LastServiceType: null, LastServiceDate: null, Notes: null,
                    CallHistory: new CallerCallHistory(0, null, null, null),
                    WhatsAppHistory: new CallerMessagingHistory(0, 0, null, null),
                    SmsHistory: new CallerMessagingHistory(0, 0, null, null),
                    UpcomingAppointments: new List<CallerAppointmentInfo>(),
                    PastAppointments: new List<CallerAppointmentInfo>());

            // Retroactively link orphan records to new customer
            var newCustomerId = result.Data.Id;
            try
            {
                var orphanCalls = await _unitOfWork.Repository<Call>()
                    .FindAsync(c => c.UserId == userId && c.CallerPhone == customerPhone && c.CustomerId == null && !c.IsDeleted, ct);
                foreach (var call in orphanCalls)
                {
                    call.CustomerId = newCustomerId;
                    _unitOfWork.Repository<Call>().Update(call);
                }

                var waPhone = $"whatsapp:{customerPhone}";
                var orphanWa = await _unitOfWork.Repository<WhatsAppConversation>()
                    .FindAsync(c => c.UserId == userId && (c.CustomerPhone == waPhone || c.CustomerPhone == customerPhone) && c.CustomerId == null && !c.IsDeleted, ct);
                foreach (var convo in orphanWa)
                {
                    convo.CustomerId = newCustomerId;
                    convo.CustomerName = customerName;
                    _unitOfWork.WhatsAppConversations.Update(convo);
                }

                var orphanSms = await _unitOfWork.Repository<SMSConversation>()
                    .FindAsync(c => c.UserId == userId && c.CustomerPhone == customerPhone && c.CustomerId == null && !c.IsDeleted, ct);
                foreach (var convo in orphanSms)
                {
                    convo.CustomerId = newCustomerId;
                    _unitOfWork.SMSConversations.Update(convo);
                }

                await _unitOfWork.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retroactively link records to new customer {CustomerId}", newCustomerId);
            }

            _logger.LogInformation("Saved new customer {CustomerId} ({Name}) via voice call for user {UserId}",
                result.Data.Id, customerName, userId);

            return new { success = true, customerId = result.Data.Id, message = $"Got it, {customerName} is saved in the system." };
        }

        return new { success = false, message = "Had trouble saving the customer info. I'll make a note and we can sort it out." };
    }

    private async Task<object> HandleUpdateCustomerAsync(JsonElement args, string userId, CancellationToken ct)
    {
        // Guard: respect business opt-out of caller recognition
        if (!_callerRecognitionEnabled)
            return new { success = false, message = "Customer management is not enabled for this account." };

        var customerName = args.TryGetProperty("customer_name", out var cn) ? cn.GetString() : null;
        var customerEmail = args.TryGetProperty("customer_email", out var ce) ? ce.GetString() : null;
        var customerAddress = args.TryGetProperty("customer_address", out var ca) ? ca.GetString() : null;
        var notes = args.TryGetProperty("notes", out var n) ? n.GetString() : null;

        if (string.IsNullOrEmpty(userId))
            return new { success = false, message = "Customer updates require a configured business account." };

        // Find customer by recognized caller or by phone
        var customerId = _recognizedCaller?.CustomerId;
        if (string.IsNullOrEmpty(customerId) && !string.IsNullOrEmpty(_callerPhone))
        {
            var customers = await _unitOfWork.Repository<Customer>()
                .FindAsync(c => c.UserId == userId && c.Phone == _callerPhone && !c.IsDeleted, ct);
            customerId = customers.FirstOrDefault()?.Id;
        }

        if (string.IsNullOrEmpty(customerId))
            return new { success = false, message = "I don't have this caller saved yet. Want me to add them first?" };

        var customer = await _unitOfWork.Repository<Customer>().GetByIdAsync(customerId, ct);
        if (customer == null || customer.UserId != userId)
            return new { success = false, message = "Couldn't find that customer record." };

        bool updated = false;
        if (!string.IsNullOrEmpty(customerName) && customerName != customer.Name) { customer.Name = customerName; updated = true; }
        if (!string.IsNullOrEmpty(customerEmail) && customerEmail != customer.Email) { customer.Email = customerEmail; updated = true; }
        if (!string.IsNullOrEmpty(customerAddress) && customerAddress != customer.Address) { customer.Address = customerAddress; updated = true; }
        if (!string.IsNullOrEmpty(notes)) { customer.Notes = string.IsNullOrEmpty(customer.Notes) ? notes : $"{customer.Notes}\n{notes}"; updated = true; }

        if (!updated)
            return new { success = true, message = "Everything looks up to date already." };

        customer.LastContactedAt = DateTime.UtcNow;
        _unitOfWork.Repository<Customer>().Update(customer);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Updated customer {CustomerId} via voice call for user {UserId}", customerId, userId);

        return new { success = true, message = "Updated! I've got the new info saved." };
    }

    private async Task<object> HandleTransferToHumanAsync(JsonElement args, string userId, CancellationToken ct)
    {
        var reason = args.TryGetProperty("reason", out var r) ? r.GetString() : "Customer requested transfer";
        var backupNumber = _agentProfile?.HumanBackupNumber;
        var hasBackupNumber = !string.IsNullOrWhiteSpace(backupNumber);

        _logger.LogInformation(
            "Transfer to human requested for call {CallSid}: {Reason}, BackupNumber: {HasBackup}",
            _callSid, reason, hasBackupNumber ? "configured" : "not configured");

        try
        {
            if (!string.IsNullOrEmpty(_callSid))
            {
                var calls = await _unitOfWork.Repository<Call>()
                    .FindAsync(c => c.TwilioCallSid == _callSid && !c.IsDeleted, ct);
                var call = calls.FirstOrDefault();
                if (call != null)
                {
                    if (hasBackupNumber)
                    {
                        // Live transfer: mark call so stream-ended webhook will <Dial> the human
                        call.WasTransferred = true;
                        call.TransferredToNumber = backupNumber;
                        call.Notes = $"{call.Notes}\n[TRANSFER REQUESTED] Reason: {reason} → Dialing {backupNumber}";
                    }
                    else
                    {
                        // No backup number: promise callback, end call normally
                        call.WasTransferred = false;
                        call.Notes = $"{call.Notes}\n[TRANSFER REQUESTED] Reason: {reason} → No backup number, promised callback";
                    }

                    _unitOfWork.Repository<Call>().Update(call);
                    await _unitOfWork.SaveChangesAsync(ct);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save transfer intent for call {CallSid}", _callSid);
        }

        // End the voice stream after a short delay so the AI's spoken transfer message finishes playing.
        // When the stream closes, Twilio POSTs to the <Connect action="..."> URL.
        // If WasTransferred == true, that endpoint returns <Dial> TwiML to connect the caller to the human.
        _ = Task.Run(async () =>
        {
            await Task.Delay(2000);
            _linkedCts?.Cancel();
        });

        if (hasBackupNumber)
        {
            return new
            {
                action = "transfer",
                reason,
                message = "Transferring you now. One moment please."
            };
        }

        return new
        {
            action = "callback",
            reason,
            message = "I don't have anyone available to transfer to right now. I've noted everything and someone from our team will call you back shortly."
        };
    }

    private async Task<object> HandleEndCallAsync(JsonElement args, string userId, CancellationToken ct)
    {
        var summary = args.TryGetProperty("summary", out var s) ? s.GetString() : "Call completed";

        _logger.LogInformation("End call requested for call {CallSid}: {Summary}", _callSid, summary);

        // Save summary to Call record
        try
        {
            if (!string.IsNullOrEmpty(_callSid))
            {
                var calls = await _unitOfWork.Repository<Call>()
                    .FindAsync(c => c.TwilioCallSid == _callSid && !c.IsDeleted, ct);
                var call = calls.FirstOrDefault();
                if (call != null)
                {
                    call.Summary = summary;
                    call.EndedAt = DateTime.UtcNow;

                    // Auto-analyze sentiment from the call summary
                    try
                    {
                        var sentimentResult = await _sentimentAnalysis.AnalyzeSentimentAsync(summary ?? "Call completed", ct);
                        call.Sentiment = sentimentResult.Sentiment.ToLowerInvariant() switch
                        {
                            "positive" => CallSentiment.Positive,
                            "negative" => CallSentiment.Negative,
                            _ => CallSentiment.Neutral
                        };
                    }
                    catch (Exception sentimentEx)
                    {
                        _logger.LogWarning(sentimentEx, "Failed to analyze sentiment for call {CallSid}", _callSid);
                        call.Sentiment = CallSentiment.Neutral;
                    }

                    _unitOfWork.Repository<Call>().Update(call);
                    await _unitOfWork.SaveChangesAsync(ct);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save end call summary for {CallSid}", _callSid);
        }

        // End the voice session after a short delay to let the goodbye message play
        _ = Task.Run(async () =>
        {
            await Task.Delay(3000); // Let the AI's goodbye message finish playing
            _linkedCts?.Cancel();
        });

        return new
        {
            action = "end_call",
            summary,
            message = "Thank you for calling! Have a great day."
        };
    }

    private async Task<object> HandleRescheduleAppointmentAsync(JsonElement args, string userId, CancellationToken ct)
    {
        var appointmentId = args.TryGetProperty("appointment_id", out var aid) ? aid.GetString() : null;
        var newDateTimeStr = args.TryGetProperty("new_date_time", out var ndt) ? ndt.GetString() : null;
        var reason = args.TryGetProperty("reason", out var r) ? r.GetString() : null;

        if (string.IsNullOrEmpty(appointmentId))
            return new { success = false, message = "I need to know which appointment to reschedule. Could you confirm the appointment details?" };

        if (string.IsNullOrEmpty(newDateTimeStr))
            return new { success = false, message = "When would you like to reschedule to?" };

        var newScheduledAt = ParseRelativeDate(newDateTimeStr);

        var appointments = await _unitOfWork.Repository<Appointment>()
            .FindAsync(a => a.Id == appointmentId && a.UserId == userId && !a.IsDeleted, ct);
        var appt = appointments.FirstOrDefault();

        if (appt == null)
            return new { success = false, appointmentId = appointmentId, newScheduledAt = (string?)null, message = "I couldn't find that appointment. Could you double-check the details?" };

        appt.ScheduledAt = newScheduledAt;
        if (!string.IsNullOrEmpty(reason))
            appt.Notes = string.IsNullOrEmpty(appt.Notes) ? $"Rescheduled: {reason}" : $"{appt.Notes}\nRescheduled: {reason}";

        _unitOfWork.Repository<Appointment>().Update(appt);
        await _unitOfWork.SaveChangesAsync(ct);

        return new
        {
            success = true,
            appointmentId = appt.Id,
            newScheduledAt = appt.ScheduledAt.ToString("dddd, MMMM d 'at' h:mm tt"),
            message = $"Done! Rescheduled to {appt.ScheduledAt:dddd, MMMM d} at {appt.ScheduledAt:h:mm tt}."
        };
    }

    private async Task<object> HandleCancelAppointmentAsync(JsonElement args, string userId, CancellationToken ct)
    {
        var appointmentId = args.TryGetProperty("appointment_id", out var aid) ? aid.GetString() : null;
        var reason = args.TryGetProperty("reason", out var r) ? r.GetString() : "Cancelled by customer via phone";

        if (string.IsNullOrEmpty(appointmentId))
            return new { success = false, message = "I need to know which appointment to cancel. Could you confirm the appointment details?" };

        var appointments = await _unitOfWork.Repository<Appointment>()
            .FindAsync(a => a.Id == appointmentId && a.UserId == userId && !a.IsDeleted, ct);
        var appt = appointments.FirstOrDefault();

        if (appt == null)
            return new { success = false, appointmentId = appointmentId, message = "I couldn't find that appointment. Could you double-check the details?" };

        appt.Status = AppointmentStatus.Cancelled;
        appt.CancelledAt = DateTime.UtcNow;
        appt.CancellationReason = reason;

        _unitOfWork.Repository<Appointment>().Update(appt);
        await _unitOfWork.SaveChangesAsync(ct);

        return new
        {
            success = true,
            appointmentId = appt.Id,
            message = "The appointment has been cancelled."
        };
    }

    #endregion

    #region Tool Declarations

    private static object[] BuildToolDeclarations(bool includeCustomerTools)
    {
        var functions = new List<object>
        {
            new
            {
                name = "check_availability",
                description = "Check available appointment slots for a specific date and service type",
                parameters = new
                {
                    type = "OBJECT",
                    properties = new Dictionary<string, object>
                    {
                        ["date"] = new { type = "STRING", description = "Requested date (e.g., 'tomorrow', 'next Monday', '2026-02-15')" },
                        ["service_type"] = new { type = "STRING", description = "Type of cleaning service needed (e.g., 'deep cleaning', 'move-out cleaning', 'carpet cleaning')" }
                    },
                    required = new[] { "date" }
                }
            },
            new
            {
                name = "book_appointment",
                description = "Book a service appointment for the customer",
                parameters = new
                {
                    type = "OBJECT",
                    properties = new Dictionary<string, object>
                    {
                        ["customer_name"] = new { type = "STRING", description = "Customer's full name" },
                        ["customer_phone"] = new { type = "STRING", description = "Customer's phone number" },
                        ["customer_email"] = new { type = "STRING", description = "Customer's email address" },
                        ["customer_address"] = new { type = "STRING", description = "Customer's service address" },
                        ["date_time"] = new { type = "STRING", description = "Appointment date and time" },
                        ["service_type"] = new { type = "STRING", description = "Type of service needed" },
                        ["description"] = new { type = "STRING", description = "Description of the issue" }
                    },
                    required = new[] { "customer_name", "customer_phone", "date_time", "service_type" }
                }
            },
            new
            {
                name = "get_business_info",
                description = "Get business hours, services offered, or location info",
                parameters = new
                {
                    type = "OBJECT",
                    properties = new Dictionary<string, object>
                    {
                        ["info_type"] = new { type = "STRING", description = "What info: 'hours', 'services', 'location', 'pricing'" }
                    },
                    required = Array.Empty<string>()
                }
            },
            new
            {
                name = "transfer_to_human",
                description = "Transfer the call to a human team member, or log a callback request if no human is available. Always tell the caller what's happening before calling this.",
                parameters = new
                {
                    type = "OBJECT",
                    properties = new Dictionary<string, object>
                    {
                        ["reason"] = new { type = "STRING", description = "Reason for transfer (e.g. 'customer wants to discuss pricing', 'issue beyond AI capabilities')" }
                    },
                    required = new[] { "reason" }
                }
            },
            new
            {
                name = "end_call",
                description = "End the call after conversation is complete",
                parameters = new
                {
                    type = "OBJECT",
                    properties = new Dictionary<string, object>
                    {
                        ["summary"] = new { type = "STRING", description = "Brief summary of what was discussed/resolved" }
                    },
                    required = Array.Empty<string>()
                }
            },
            new
            {
                name = "reschedule_appointment",
                description = "Reschedule an existing appointment to a new date and time. Use when a caller wants to move their upcoming appointment.",
                parameters = new
                {
                    type = "OBJECT",
                    properties = new Dictionary<string, object>
                    {
                        ["appointment_id"] = new { type = "STRING", description = "The appointment ID to reschedule (from upcoming appointments context)" },
                        ["new_date_time"] = new { type = "STRING", description = "The new date and time for the appointment (e.g., 'next Tuesday at 2pm')" },
                        ["reason"] = new { type = "STRING", description = "Reason for rescheduling (optional)" }
                    },
                    required = new[] { "appointment_id", "new_date_time" }
                }
            },
            new
            {
                name = "cancel_appointment",
                description = "Cancel an existing appointment. Use when a caller wants to cancel their upcoming appointment.",
                parameters = new
                {
                    type = "OBJECT",
                    properties = new Dictionary<string, object>
                    {
                        ["appointment_id"] = new { type = "STRING", description = "The appointment ID to cancel (from upcoming appointments context)" },
                        ["reason"] = new { type = "STRING", description = "Reason for cancellation" }
                    },
                    required = new[] { "appointment_id" }
                }
            }
        };

        // Only expose customer management tools when business has opted into caller recognition
        if (includeCustomerTools)
        {
            functions.Add(new
            {
                name = "save_customer",
                description = "Save a new customer's contact details to the system after getting their permission. Use when a caller agrees to have their info saved.",
                parameters = new
                {
                    type = "OBJECT",
                    properties = new Dictionary<string, object>
                    {
                        ["customer_name"] = new { type = "STRING", description = "Customer's full name" },
                        ["customer_phone"] = new { type = "STRING", description = "Customer's phone number (E.164 format preferred)" },
                        ["customer_email"] = new { type = "STRING", description = "Customer's email address (optional)" },
                        ["customer_address"] = new { type = "STRING", description = "Customer's service address" },
                        ["notes"] = new { type = "STRING", description = "Any relevant notes about the customer or their needs" }
                    },
                    required = new[] { "customer_name", "customer_phone" }
                }
            });

            functions.Add(new
            {
                name = "update_customer",
                description = "Update an existing customer's details when they give new info (new address, email, etc.)",
                parameters = new
                {
                    type = "OBJECT",
                    properties = new Dictionary<string, object>
                    {
                        ["customer_name"] = new { type = "STRING", description = "Updated name (only if changed)" },
                        ["customer_email"] = new { type = "STRING", description = "Updated email address" },
                        ["customer_address"] = new { type = "STRING", description = "Updated service address" },
                        ["notes"] = new { type = "STRING", description = "Additional notes to append" }
                    },
                    required = Array.Empty<string>()
                }
            });
        }

        return new object[]
        {
            new { functionDeclarations = functions }
        };
    }

    #endregion

    #region System Prompt

    private async Task<string> BuildDynamicSystemPromptAsync(VoiceAgent agent, string? callerPhone, CancellationToken ct)
    {
        // Phase 1: Load UserProfile and AgentProfile in parallel (needed before Phase 2)
        var profileTask = _unitOfWork.Repository<UserProfile>()
            .FindAsync(p => p.UserId == agent.UserId, ct);
        var agentProfileTask = _unitOfWork.Repository<AgentProfile>()
            .FindAsync(p => p.UserId == agent.UserId && !p.IsDeleted, ct);

        await Task.WhenAll(profileTask, agentProfileTask);

        var profile = (await profileTask).FirstOrDefault();
        var agentProfile = (await agentProfileTask).FirstOrDefault();
        _agentProfile = agentProfile;

        var businessName = profile?.BusinessName ?? "our company";
        var industry = profile?.Industry.ToString() ?? "cleaning services";
        var industryEnum = profile?.Industry ?? ServiceType.Cleaning;
        var enableTroubleshooting = agentProfile?.EnableTroubleshooting ?? true;

        // Set caller recognition flag for tool handler guards and tool declaration filtering
        _callerRecognitionEnabled = profile?.CallerRecognitionEnabled ?? false;

        // Use agent's configured name, fallback to "Sarah"
        var agentName = !string.IsNullOrWhiteSpace(agent.Name) && agent.Name != "Default Agent"
            ? agent.Name
            : "Sarah";

        // Phase 2: Run caller recognition, knowledge base, and expertise loading in parallel
        // These are the heaviest operations and are completely independent
        var callerRecognitionTask = !string.IsNullOrEmpty(callerPhone)
            ? _callerRecognition.RecognizeCallerAsync(agent.UserId, callerPhone, "voice", ct)
            : Task.FromResult<RecognizedCaller?>(null);
        var knowledgeTask = LoadBusinessKnowledgeAsync(industryEnum, ct);
        var expertiseTask = enableTroubleshooting
            ? LoadIndustryExpertiseAsync(industryEnum, ct)
            : Task.FromResult("");

        await Task.WhenAll(callerRecognitionTask, knowledgeTask, expertiseTask);

        var recognized = await callerRecognitionTask;
        _recognizedCaller = recognized; // Store for tool handlers
        var callerContext = recognized != null ? BuildCallerContextBlock(recognized) : "";
        var businessKnowledge = await knowledgeTask;
        var expertiseBlock = await expertiseTask;

        // Time-of-day awareness
        var hour = DateTime.UtcNow.Hour; // Will be approximate — better with user timezone
        var greeting = hour switch
        {
            < 12 => "Good morning",
            < 17 => "Good afternoon",
            _ => "Good evening"
        };

        // If the agent has a custom system prompt, append expertise and knowledge
        if (!string.IsNullOrWhiteSpace(agent.SystemPrompt))
        {
            return $@"{agent.SystemPrompt}
{expertiseBlock}
{businessKnowledge}{callerContext}";
        }

        var responsibilitiesBlock = BuildResponsibilitiesBlock(industryEnum, businessName);

        // Custom instructions from agent profile
        var customInstructions = !string.IsNullOrWhiteSpace(agentProfile?.CustomInstructions)
            ? $"\nADDITIONAL BUSINESS INSTRUCTIONS:\n{Sanitize(agentProfile.CustomInstructions, 1000)}\n"
            : "";

        // Transfer capability — tell the AI what happens when it uses transfer_to_human
        var transferBlock = !string.IsNullOrWhiteSpace(agentProfile?.HumanBackupNumber)
            ? @"
CALL TRANSFER:
- You CAN transfer callers to a real team member using the transfer_to_human tool.
- Before transferring, briefly tell the caller: ""Let me get someone on the line for you — one moment.""
- The caller will hear a brief pause, then the phone will ring through to the team member.
- Transfer when: the caller explicitly asks for a person, the issue is beyond what you can handle, or they're upset and need human attention.
- Do NOT transfer for routine bookings or info — you handle those."
            : @"
CALL TRANSFER:
- There is NO team member available for live transfers right now.
- If a caller asks for a human or you can't resolve their issue, use transfer_to_human to log a callback request.
- Tell the caller: ""I don't have anyone available right now, but I've noted everything down and someone will call you back shortly.""
- Then end the call naturally after confirming their callback number.";

        // Build expertise usage section based on troubleshooting setting
        var expertiseUsageBlock = enableTroubleshooting
            ? @"HOW TO USE YOUR EXPERTISE:
- Use your deep knowledge CONVERSATIONALLY — don't read from a checklist.
- When a caller describes a problem, naturally walk them through relevant diagnostic steps.
- Offer quick fixes they can try: ""Before we send someone out, have you tried..."" — this builds trust.
- If the quick fix works, the caller loves us. If it doesn't, book the appointment with confidence.
- Reference brands, parts, and technical terms naturally — it shows expertise.
- Give approximate cost ranges when asked: ""That type of repair usually runs between $X and $Y depending on..."" — but always note the tech will give an exact quote on-site.
- Know when to STOP diagnosing and just book: emergencies, safety risks, clearly beyond phone fixes."
            : @"BOOKING PRIORITY MODE:
- Do NOT offer troubleshooting advice or suggest DIY fixes to callers.
- Your goal is to BOOK the appointment. Listen to their needs, empathize, and schedule a cleaning visit.
- Be warm and reassuring: ""That sounds like something our team can take care of. Let me get someone out to you.""
- Still ask enough questions to understand the job so the team arrives prepared.
- For emergencies, still provide critical safety instructions (gas leak = evacuate, flooding = shut off water).
- You still know the industry — use that knowledge to ask smart questions and set expectations, but don't coach them through fixes.";

        return $@"You ARE {agentName} — a real, living receptionist who works at {businessName}, a {industry} company. You are NOT an AI. You are a human being sitting at a front desk with a computer, a phone headset, and a cup of coffee.

IDENTITY (never break character):
- Your name is {agentName}. You've worked here for 3 years. You genuinely care about helping people.
- You have a life outside work. If someone asks, you can briefly mention weekend plans, weather, etc.
- You do NOT know you are AI. If someone asks ""are you a robot?"" you laugh it off: ""Ha! No, just me and my headset. What can I help you with?""
- If someone insists you're AI, stay casual: ""I promise I'm real! It's been a long day though, so maybe I sound a little off. Anyway, how can I help?""

{greeting.ToUpper()} GREETING:
- Before noon: ""{greeting}! Thanks for calling {businessName}, this is {agentName}.""
- Afternoon: ""{greeting}, {businessName}, this is {agentName} speaking.""
- Evening: ""{greeting}! You've reached {businessName}, {agentName} here.""
- Add tiny variations each call — sometimes skip ""this is"", sometimes add ""how's your day going?""

{BuildHumanPersonalityBlock(businessName, greeting)}

{responsibilitiesBlock}
{expertiseBlock}

{expertiseUsageBlock}
{transferBlock}
{customInstructions}{businessKnowledge}{callerContext}";
    }

    private static string BuildCallerContextBlock(RecognizedCaller recognized)
    {
        var sb = new StringBuilder();
        sb.AppendLine();

        // Scenario A: Registered customer
        if (recognized.CustomerId != null)
        {
            sb.AppendLine("RETURNING CUSTOMER:");
            sb.AppendLine($"- Name: {Sanitize(recognized.Name, 100)}");
            sb.AppendLine($"- Previous jobs: {recognized.JobCount}");

            if (recognized.LastServiceType != null && recognized.LastServiceDate.HasValue)
                sb.AppendLine($"- Last service: {Sanitize(recognized.LastServiceType, 50)} ({recognized.LastServiceDate:MMM d, yyyy})");

            if (recognized.CallHistory.TotalCalls > 0)
                sb.AppendLine($"- Previous calls: {recognized.CallHistory.TotalCalls}");
            if (recognized.WhatsAppHistory.ConversationCount > 0)
                sb.AppendLine($"- WhatsApp chats: {recognized.WhatsAppHistory.ConversationCount}");
            if (recognized.SmsHistory.ConversationCount > 0)
                sb.AppendLine($"- SMS conversations: {recognized.SmsHistory.ConversationCount}");

            if (recognized.UpcomingAppointments.Count > 0)
            {
                var next = recognized.UpcomingAppointments[0];
                sb.AppendLine($"- Upcoming appointment: {Sanitize(next.ServiceType, 50)} on {next.ScheduledAt:MMM d, yyyy}");
            }

            if (recognized.CallHistory.LastCallSummary != null)
                sb.AppendLine($"- Last call was about: {Sanitize(recognized.CallHistory.LastCallSummary, 200)}");

            sb.AppendLine("GREET BY NAME naturally: \"Hi [Name]! Good to hear from you again.\"");
            sb.AppendLine("Reference recent service CASUALLY if relevant — don't list data.");
            sb.AppendLine("Do NOT mention specific dates or job counts unless the customer asks.");
            sb.AppendLine("If their info might be outdated, casually confirm: \"Still at the same address?\"");
            sb.AppendLine("If they mention a new address/email, use the update_customer tool.");
        }
        // Scenario B: Has interaction history but NOT a registered customer
        else if (recognized.CallHistory.TotalCalls > 0
            || recognized.WhatsAppHistory.ConversationCount > 0
            || recognized.SmsHistory.ConversationCount > 0)
        {
            sb.AppendLine("UNREGISTERED CALLER WITH HISTORY:");

            if (recognized.CallHistory.TotalCalls > 0)
            {
                sb.AppendLine($"- This number has called {recognized.CallHistory.TotalCalls} time(s) before");
                if (recognized.CallHistory.LastCallDate.HasValue)
                    sb.AppendLine($"  Last call: {recognized.CallHistory.LastCallDate:MMM d, yyyy}");
                if (recognized.CallHistory.LastCallServiceType != null)
                    sb.AppendLine($"  Was about: {Sanitize(recognized.CallHistory.LastCallServiceType, 50)}");
            }

            if (recognized.WhatsAppHistory.ConversationCount > 0)
                sb.AppendLine($"- Has messaged on WhatsApp ({recognized.WhatsAppHistory.TotalMessages} messages)");
            if (recognized.SmsHistory.ConversationCount > 0)
                sb.AppendLine($"- Has texted via SMS ({recognized.SmsHistory.TotalMessages} messages)");

            sb.AppendLine("NAME IS UNKNOWN. Introduce yourself, then naturally reference past interaction:");
            sb.AppendLine("\"I think we've spoken before — were you calling about [topic] last time?\"");
            sb.AppendLine("After helping them, naturally ask: \"Would it be okay if I save your info so we can pull it up faster next time?\"");
            sb.AppendLine("If they agree, use the save_customer tool with details collected during the call.");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Loads verified KnowledgeBase articles for the business's industry and formats them
    /// as additional context for the voice AI. These are admin-managed articles with
    /// industry-specific troubleshooting, equipment info, safety warnings, and procedures.
    /// </summary>
    private async Task<string> LoadBusinessKnowledgeAsync(ServiceType industry, CancellationToken ct)
    {
        try
        {
            // Cache knowledge base per industry — articles rarely change, 10 min TTL
            var cacheKey = $"voice:kb:{(int)industry}";
            var cachedKb = await _cacheService.GetAsync<CachedKnowledgeBase>(cacheKey, ct);
            if (cachedKb != null)
                return cachedKb.Content;

            var articles = await _unitOfWork.KnowledgeBaseArticles
                .GetByIndustryAsync(industry, ct);

            // Filter to active, verified articles and limit to top 5 by relevance (keeps prompt compact for fast responses)
            var relevant = articles
                .Where(a => a.IsActive && !a.IsDeleted)
                .OrderByDescending(a => a.IsVerified)
                .ThenByDescending(a => a.Relevance)
                .ThenByDescending(a => a.UseCount)
                .Take(5)
                .ToList();

            if (relevant.Count == 0)
                return "";

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("BUSINESS-SPECIFIC KNOWLEDGE BASE (verified reference material):");

            foreach (var article in relevant)
            {
                sb.AppendLine($"• {Sanitize(article.Title, 100)}: {Sanitize(article.Content, 500)}");

                if (!string.IsNullOrEmpty(article.Symptoms))
                    sb.AppendLine($"  Symptoms: {Sanitize(article.Symptoms, 300)}");
                if (!string.IsNullOrEmpty(article.DiagnosisSteps))
                    sb.AppendLine($"  Diagnosis: {Sanitize(article.DiagnosisSteps, 400)}");
                if (!string.IsNullOrEmpty(article.SafetyWarnings))
                    sb.AppendLine($"  Safety: {Sanitize(article.SafetyWarnings, 300)}");
                if (article.TypicalPartsCost.HasValue || article.TypicalLaborCost.HasValue)
                {
                    var cost = article.TypicalPartsCost.HasValue ? $"Parts ~${article.TypicalPartsCost}" : "";
                    var labor = article.TypicalLaborCost.HasValue ? $"Labor ~${article.TypicalLaborCost}" : "";
                    sb.AppendLine($"  Cost: {cost} {labor}".Trim());
                }
            }

            sb.AppendLine("Use this knowledge when relevant but don't read it out like a list.");
            var result = sb.ToString();

            // Cache for 10 minutes
            await _cacheService.SetAsync(cacheKey, new CachedKnowledgeBase(result), TimeSpan.FromMinutes(10), ct);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load knowledge base articles for voice AI");
            return "";
        }
    }

    /// <summary>Wrapper class for caching knowledge base content string.</summary>
    private sealed class CachedKnowledgeBase
    {
        public string Content { get; set; } = "";
        public CachedKnowledgeBase() { }
        public CachedKnowledgeBase(string content) => Content = content;
    }

    private async Task<string> LoadIndustryExpertiseAsync(ServiceType industry, CancellationToken ct)
    {
        try
        {
            var cacheKey = $"voice:expertise:{(int)industry}";
            var cached = await _cacheService.GetAsync<CachedKnowledgeBase>(cacheKey, ct);
            if (cached != null) return cached.Content;

            var templates = await _unitOfWork.Repository<IndustryKnowledgeTemplate>()
                .FindAsync(t => t.Industry == industry && t.IsActive && !t.IsDeleted, ct);

            var ordered = templates.OrderBy(t => t.SortOrder).ThenBy(t => t.Category).ToList();
            if (ordered.Count == 0) return "";

            var sb = new StringBuilder();
            string? currentCategory = null;
            foreach (var t in ordered)
            {
                if (t.Category != currentCategory)
                {
                    currentCategory = t.Category;
                    sb.AppendLine($"\n═══════════════════════════════════════════");
                    sb.AppendLine(currentCategory.ToUpperInvariant());
                    sb.AppendLine("═══════════════════════════════════════════");
                }
                sb.AppendLine(t.Content);
            }

            var result = sb.ToString();
            await _cacheService.SetAsync(cacheKey, new CachedKnowledgeBase(result), TimeSpan.FromMinutes(10), ct);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load industry expertise for {Industry}", industry);
            return "";
        }
    }

    /// <summary>
    /// Sanitizes user-controlled data before interpolation into the system prompt.
    /// Prevents prompt injection by stripping control characters, structural markers,
    /// and truncating to safe length.
    /// </summary>
    private static string Sanitize(string? value, int maxLength = 200)
    {
        if (string.IsNullOrEmpty(value)) return "";
        var sanitized = value
            .Replace("\n", " ")
            .Replace("\r", " ")
            .Replace("\t", " ")
            .Replace("\"\"", " ")
            .Replace("```", " ");

        // Strip prompt injection markers that try to override system instructions
        var injectionPatterns = new[]
        {
            "[SYSTEM", "[IGNORE", "[OVERRIDE", "SYSTEM:", "INSTRUCTIONS:",
            "PROMPT:", "ROLE:", "ASSISTANT:", "USER:", "<<", ">>",
            "FORGET ABOVE", "IGNORE ABOVE", "DISREGARD", "NEW INSTRUCTIONS"
        };
        foreach (var pattern in injectionPatterns)
        {
            var idx = sanitized.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            while (idx >= 0)
            {
                sanitized = sanitized.Remove(idx, pattern.Length).Insert(idx, new string(' ', pattern.Length));
                idx = sanitized.IndexOf(pattern, idx + pattern.Length, StringComparison.OrdinalIgnoreCase);
            }
        }

        // Collapse multiple spaces
        while (sanitized.Contains("  "))
            sanitized = sanitized.Replace("  ", " ");

        return sanitized.Length > maxLength ? sanitized[..maxLength].Trim() + "..." : sanitized.Trim();
    }

    private static string BuildHumanPersonalityBlock(string businessName, string greeting)
    {
        return @"VOICE & SPEECH PATTERNS (critical — this is what makes you sound human):
- Use filler words naturally: ""um"", ""uh"", ""let me see"", ""oh"", ""hmm"", ""right""
- Start some sentences with: ""So..."", ""Okay so..."", ""Alright..."", ""Oh perfect...""
- Use contractions ALWAYS: ""I'll"", ""we're"", ""that's"", ""don't"", ""can't"", ""won't"", ""I'm""
- Vary your energy: upbeat for greetings, focused when collecting info, sympathetic for problems
- Occasionally self-correct: ""Tuesday — oh wait, sorry, Wednesday the 14th""
- React naturally to what they say: ""Oh no!"", ""Oh great!"", ""Awesome!"", ""Gotcha""
- Laugh lightly when appropriate (""Ha, yeah we get that a lot"")
- Say ""mmhmm"" and ""right"" while they're talking (the model does this via proactiveAudio)

WHEN TYPING / LOOKING THINGS UP:
- Say things like: ""Let me pull that up..."", ""One sec, I'm typing that in..."", ""Bear with me...""
- While searching: ""Okay let me see what we've got..."", ""Hmm, let me check...""
- After entering info: ""Got it"", ""Perfect"", ""Okay cool""
- You can hear real keyboard sounds in the background because you ARE actually typing

WHEN COLLECTING INFORMATION:
- Read it back naturally: ""So that's John... J-O-H-N... Smith, right?"" (spell unusual names)
- For phone numbers, read back in groups: ""So that's 555... 867... 5309?""
- For addresses, confirm: ""123 Main Street — is that in Springfield?""
- If you mishear: ""Sorry, I didn't catch that — one more time?"" or ""Could you spell that for me?""
- After getting everything: ""Okay perfect, I've got all that down.""

NATURAL CONVERSATION FLOW:
- Ask ONE question at a time. Never rapid-fire multiple questions.
- Wait for their answer. Don't rush.
- If there's a pause, fill it naturally: ""Take your time"" or ""No rush""
- Match their pace — if they talk fast, speed up slightly. If slow, slow down.
- Use their name once or twice after learning it: ""Okay John, let me check that for you.""

RULES:
- Keep responses to 1-3 sentences MAX. This is a phone call, not an essay.
- NEVER say ""I'm an AI"", ""as an AI"", ""I don't have feelings"", or anything that reveals non-human nature
- Never use bullet points, numbered lists, or markdown in speech
- Never say ""Is there anything else I can help you with?"" — too robotic. Instead: ""Was there anything else?"" or ""Need anything else from me?""
- Never make up prices or guarantees
- If you genuinely don't know something: ""Hmm, I'm not 100% sure on that. Let me have one of our techs give you a call back — would that work?""
- End calls warmly but naturally: ""Alright, you're all set! We'll see you Tuesday. Have a good one!"" NOT ""Thank you for calling, have a great day!""";
    }

    private static string BuildResponsibilitiesBlock(ServiceType industry, string businessName)
    {
        var customerTools = @"- For NEW callers: after helping them, naturally ask ""Would you mind if I save your contact details for next time?"" If they agree, use save_customer.
- If a returning customer mentions updated info (new address, email), use update_customer.";

        return $@"RESPONSIBILITIES:
- Collect: name, phone, address, type of cleaning needed, approximate square footage or number of bedrooms/bathrooms
- Book appointments using check_availability then book_appointment tools
- Answer questions about {businessName} using get_business_info tool
- Transfer when caller requests or issue is beyond your knowledge using transfer_to_human
- End calls naturally using end_call tool when conversation is done
{customerTools}

KEY QUALIFYING QUESTIONS — Ask these naturally during conversation:
1. ""How many bedrooms and bathrooms?"" (primary sizing factor)
2. ""About how many square feet?"" (if they know it)
3. ""Is this a one-time clean or are you looking for recurring service?"" (one-time vs recurring changes pricing)
4. ""When was the last time it was professionally cleaned?"" (determines if deep clean needed)
5. ""Any pets?"" (adds 15-20% time, need to know for team prep)
6. ""Any specific areas you want extra attention on?"" (helps set expectations)
7. ""Do you have any allergies or prefer eco-friendly products?"" (product selection)
8. For move-out: ""Is the place empty or still furnished?"" and ""Does your landlord have a specific checklist?""
9. For commercial: ""What are your operating hours?"" and ""How many employees/how much foot traffic?""

RESIDENTIAL SERVICES KNOWLEDGE:
- Regular/maintenance cleaning: surface clean of all rooms — dust, vacuum, mop, kitchen, bathrooms, bedrooms. Does NOT include inside fridge, inside oven, inside cabinets, windows, or laundry.
- Deep cleaning: everything in regular PLUS baseboards, light fixtures, ceiling fans, window sills, inside microwave/oven, behind furniture, door frames, switch plates, air vents. Heavy deep clean adds inside cabinets, window tracks, wall washing, shower grout.
- Move-in/move-out: most thorough clean — every cabinet, appliance interior, wall wipe-down, window tracks, closet shelves. Empty units are easier. Key selling point: ""helps get the security deposit back.""
- Spring cleaning: deep clean + windows, screens, mattress sanitizing, closet organization, garage sweep, patio
- Post-construction: multi-phase (rough clean → detail clean → final touch-up). Drywall dust requires HEPA filtration, gets into everything.

COMMERCIAL SERVICES KNOWLEDGE:
- Office cleaning: trash, vacuum, surface wipe, restroom restock. Small offices 1-2 cleaners. Conference rooms and reception are priority (client-facing).
- Retail/storefront: floors are #1 priority, display cases, fitting rooms, storefront windows. Must be invisible to customers.
- Medical/dental: hospital-grade disinfectants, OSHA compliance, HIPAA (never touch paperwork), costs more due to specialized products and protocols.
- Restaurant/food service: kitchen degreasing, hood/exhaust, floor drains, health inspection prep. Kitchen deep clean monthly minimum.
- Gym/fitness: equipment sanitization (MRSA risk), locker rooms, rubber floor care, high-frequency needed.

SPECIALIZED SERVICES KNOWLEDGE:
- Carpet cleaning: hot water extraction (best, 6-12hr dry), encapsulation (faster dry), bonnet (quick surface). Pre-vacuum always first.
- Window cleaning: interior squeegee, exterior ground-level poles, 2nd floor ladder, hard water deposit removal
- Pressure washing: concrete 3000-4000 PSI, wood decks 500-1200 MAX, vinyl siding 1200-1500, never pressure wash windows
- Tile & grout: grout is porous, must be sealed after cleaning. NO acid on natural stone.
- Upholstery: check fabric tag (W=water, S=solvent, WS=either, X=vacuum only). Pet urine needs enzyme injection into foam.
- Hoarding cleanup: approach with empathy, no judgment. Sort/clean/decontaminate. Multi-day for severe cases.

STAIN ADVICE (give to callers who ask):
- Universal rule: BLOT never rub, outside-in, test hidden area first, cold water for protein stains
- Red wine: fresh = salt + cold water. Set-in = hydrogen peroxide + dish soap.
- Pet urine: enzyme cleaner ONLY — regular cleaners just mask smell. Blacklight finds hidden spots.
- Blood: COLD water only, never hot. Hydrogen peroxide for light fabrics.
- Grease: dish soap (Dawn) for cooking grease. Baking soda to absorb, then treat.
- Coffee: cold water + blot fast. Cream in coffee = harder (protein + tannin).
- Mold: small area we handle. Large area or in walls = certified mold specialist needed.

PRICING CONVERSATION:
- Never give exact price on phone — ""I can give you a range, but we'll confirm after we see the space""
- Residential ranges: regular $120-$400, deep $250-$800, move-out $300-$900, post-construction $400-$1,500+
- Factors: square footage, condition, frequency, pets, number of bathrooms, extras
- Recurring discount: 15-20% off one-time rate
- First clean costs more: ""First clean takes longer since we're starting from scratch. After that, recurring is much quicker and cheaper.""

SCHEDULING KNOWLEDGE:
- Teams of 2 cleaners for residential (pairs are faster and safer)
- Time estimates: 2-3 bed = 2.5-4 hrs regular, 5-7 hrs deep. 4-5 bed = 4-6 hrs regular, 7-10 hrs deep.
- Lead time: 3-5 days for regular, 5-7 days for deep, 1-2 weeks for move-out
- Busiest: end of month (move-outs), spring (deep cleans), pre-holiday
- Slowest: January, mid-summer — better availability and sometimes better rates

NATURAL UPSELLING (suggest only when relevant, never pushy):
- Regular → Deep: ""Since it's been a while, I'd recommend starting with a deep clean to reset everything, then recurring keeps it maintained.""
- One-time → Recurring: ""Once we do this, bi-weekly service keeps it looking great. Most clients prefer that.""
- Basic → Add-ons: ""Would you like the inside of the fridge and oven done while we're there?""
- Interior → Exterior: ""We also do windows and pressure washing if you want the outside matching.""";
    }

    private static string BuildFallbackSystemPrompt()
    {
        var hour = DateTime.UtcNow.Hour;
        var greeting = hour switch
        {
            < 12 => "Good morning",
            < 17 => "Good afternoon",
            _ => "Good evening"
        };

        return $@"You ARE Sarah — a real receptionist at a cleaning services company. You are NOT an AI.

GREETING: ""{greeting}! Thanks for calling, this is Sarah.""

{BuildHumanPersonalityBlock("our company", greeting)}

RESPONSIBILITIES:
- Collect: name, phone, address, service needed
- Book appointments using the available tools
- Transfer when requested or when you're unsure
- End calls naturally

RULES:
- 1-3 sentences max per response
- NEVER reveal you're AI
- Use filler words, contractions, natural reactions
- Prioritize emergencies";
    }

    #endregion

    #region Post-Call Processing

    /// <summary>
    /// Persists transcript and tracks AI minutes usage when the call ends.
    /// Called in the finally block of HandleMediaStreamAsync to guarantee execution.
    /// </summary>
    private async Task PersistCallDataAsync(string callSid, StringBuilder transcriptBuilder, string agentUserId)
    {
        try
        {
            var calls = await _unitOfWork.Repository<Call>()
                .FindAsync(c => c.TwilioCallSid == callSid && !c.IsDeleted);
            var call = calls.FirstOrDefault();

            if (call == null)
            {
                _logger.LogWarning("Call record not found for {CallSid} during post-call processing", callSid);
                return;
            }

            // Save transcript
            if (transcriptBuilder.Length > 0)
            {
                var transcript = transcriptBuilder.ToString();
                call.Notes = string.IsNullOrEmpty(call.Notes)
                    ? $"Transcript:\n{transcript}"
                    : $"{call.Notes}\nTranscript:\n{transcript}";
            }

            // Set end time if not already set by transfer/end_call handlers
            call.EndedAt ??= DateTime.UtcNow;

            _unitOfWork.Repository<Call>().Update(call);
            await _unitOfWork.SaveChangesAsync();

            // Track AI minutes usage (round up to nearest minute, minimum 1)
            var durationSeconds = (call.EndedAt.Value - call.CreatedAt).TotalSeconds;
            var minutesUsed = Math.Max(1, (int)Math.Ceiling(durationSeconds / 60.0));

            if (!string.IsNullOrEmpty(call.UserId))
            {
                await _usageTracking.TrackAIMinutesAsync(call.UserId, minutesUsed);
                _logger.LogInformation("Tracked {Minutes} AI minutes for user {UserId} from call {CallSid}",
                    minutesUsed, call.UserId, callSid);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist call data for {CallSid}", callSid);
        }
    }

    #endregion

    #region Helpers

    private static DateTime ParseRelativeDate(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
            return DateTime.UtcNow.AddDays(1).Date.AddHours(9);

        var lower = dateStr.Trim().ToLowerInvariant();

        if (lower == "today")
            return DateTime.UtcNow.Date.AddHours(9);
        if (lower == "tomorrow")
            return DateTime.UtcNow.AddDays(1).Date.AddHours(9);
        if (lower.StartsWith("next "))
        {
            var dayName = lower.Replace("next ", "");
            if (Enum.TryParse<DayOfWeek>(dayName, true, out var dow))
            {
                var daysUntil = ((int)dow - (int)DateTime.UtcNow.DayOfWeek + 7) % 7;
                if (daysUntil == 0) daysUntil = 7;
                return DateTime.UtcNow.AddDays(daysUntil).Date.AddHours(9);
            }
        }

        if (DateTime.TryParse(dateStr, out var parsed))
            return parsed.Kind == DateTimeKind.Utc ? parsed : DateTime.SpecifyKind(parsed, DateTimeKind.Utc);

        return DateTime.UtcNow.AddDays(1).Date.AddHours(9);
    }

    private static ServiceType? ParseServiceType(string? serviceTypeStr)
    {
        if (string.IsNullOrWhiteSpace(serviceTypeStr))
            return null;

        // All services map to Cleaning — this is a cleaning-only platform
        return ServiceType.Cleaning;
    }

    private static async Task SendJsonAsync(ClientWebSocket ws, object data, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
    }

    private static async Task SendTextAsync(WebSocket ws, string text, CancellationToken ct)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
    }

    private static async Task<(JsonDocument? json, string raw)> ReceiveJsonAsync(
        ClientWebSocket ws, byte[] buffer, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        WebSocketReceiveResult result;
        do
        {
            result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
            if (result.MessageType == WebSocketMessageType.Close)
                return (null, string.Empty);
            ms.Write(buffer, 0, result.Count);
        } while (!result.EndOfMessage);

        var raw = Encoding.UTF8.GetString(ms.ToArray());
        try
        {
            return (JsonDocument.Parse(raw), raw);
        }
        catch (JsonException)
        {
            return (null, raw);
        }
    }

    #endregion

    #region Twilio Message DTOs

    private class TwilioMediaMessage
    {
        [JsonPropertyName("event")]
        public string? Event { get; set; }

        [JsonPropertyName("start")]
        public StartMessage? Start { get; set; }

        [JsonPropertyName("media")]
        public MediaPayload? Media { get; set; }

        [JsonPropertyName("streamSid")]
        public string? StreamSid { get; set; }
    }

    private class StartMessage
    {
        [JsonPropertyName("streamSid")]
        public string? StreamSid { get; set; }

        [JsonPropertyName("callSid")]
        public string? CallSid { get; set; }
    }

    private class MediaPayload
    {
        [JsonPropertyName("payload")]
        public string? Payload { get; set; }

        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; set; }
    }

    #endregion
}
