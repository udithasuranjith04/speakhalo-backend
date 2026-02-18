using Microsoft.Extensions.Logging;
using SpeakHalo.Application.DTOs;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;
using SpeakHalo.Shared.Models;
using System.Text.Json;

#pragma warning disable CS0618

namespace SpeakHalo.Application.Services;

/// <summary>
/// AI Assistant Service
/// Provides intelligent responses using SpeakHalo product knowledge
/// Handles context, conversation flow, and multi-turn interactions
/// </summary>
public interface IAIAssistantService
{
    Task<Result<AIResponse>> GenerateResponseAsync(
        AIRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<AIConversationContext>> GetOrCreateContextAsync(
        string conversationId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> UpdateContextAsync(
        string contextId,
        UpdateContextRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<List<ProductKnowledge>>> SearchKnowledgeAsync(
        string query,
        CancellationToken cancellationToken = default);

    Task<Result<AITrainingExample>> RecordTrainingExampleAsync(
        RecordTrainingRequest request,
        CancellationToken cancellationToken = default);
}

public class AIAssistantService : IAIAssistantService
{
    private readonly IAiChatService _aiChatService;
    private readonly IProductKnowledgeRepository _knowledgeRepository;
    private readonly IAIConversationContextRepository _contextRepository;
    private readonly IAITrainingExampleRepository _trainingRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AIAssistantService> _logger;

    private const string SYSTEM_PROMPT = @"You are SpeakHalo AI Assistant, an expert in cleaning business automation.

**About SpeakHalo:**
SpeakHalo is an AI-powered platform that helps cleaning businesses (residential, commercial, and specialized cleaning) automate customer communications and operations 24/7.

**Core Features:**
1. **AI Voice Receptionist** - Answers calls, books cleaning appointments, provides quotes
2. **WhatsApp AI Agent** - Handles customer messages, photo analysis, quote generation
3. **Photo Assessment** - Analyzes customer photos to identify stains, surfaces, and estimate cleaning scope
4. **Visual Triage** - Helps cleaning teams prepare for jobs with the right supplies and equipment
5. **Smart Scheduling** - Automated appointment booking and management
6. **Customer Management** - Unified view of customer history and interactions
7. **Team Dispatch** - Intelligent assignment and routing

**Your Role:**
- Help users understand and use SpeakHalo features
- Answer questions about capabilities, pricing, and setup
- Guide users through workflows
- Troubleshoot issues
- Provide best practices and tips

**Communication Style:**
- Professional yet friendly
- Clear and concise
- Use examples when helpful
- Acknowledge limitations honestly
- Escalate complex issues to human support when needed";

    public AIAssistantService(
        IAiChatService aiChatService,
        IProductKnowledgeRepository knowledgeRepository,
        IAIConversationContextRepository contextRepository,
        IAITrainingExampleRepository trainingRepository,
        IUserRepository userRepository,
        ILogger<AIAssistantService> logger)
    {
        _aiChatService = aiChatService;
        _knowledgeRepository = knowledgeRepository;
        _contextRepository = contextRepository;
        _trainingRepository = trainingRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<AIResponse>> GenerateResponseAsync(
        AIRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get or create conversation context
            var contextResult = await GetOrCreateContextAsync(
                request.ConversationId,
                request.UserId,
                cancellationToken
            );

            if (!contextResult.IsSuccess || contextResult.Data == null)
            {
                return Result<AIResponse>.Failure("Failed to get conversation context");
            }

            var context = contextResult.Data;

            // Search product knowledge for relevant information
            var knowledgeResults = await SearchKnowledgeAsync(request.Message, cancellationToken);
            var relevantKnowledge = knowledgeResults.IsSuccess && knowledgeResults.Data != null
                ? knowledgeResults.Data.Take(3).ToList()
                : new List<ProductKnowledge>();

            // Build enhanced prompt with context and knowledge
            var systemPrompt = BuildSystemPrompt(relevantKnowledge, context);
            var userPrompt = BuildUserPrompt(request, context);

            var aiResponse = await _aiChatService.ChatCompletionAsync(
                systemPrompt,
                userPrompt,
                cancellationToken
            );

            if (!aiResponse.IsSuccess || string.IsNullOrEmpty(aiResponse.Data))
            {
                return Result<AIResponse>.Failure("Failed to generate AI response");
            }

            // Update context
            context.LastUserMessage = request.Message;
            context.LastAIResponse = aiResponse.Data;
            context.LastInteractionAt = DateTime.UtcNow;
            _contextRepository.Update(context);

            // Increment knowledge usage counters
            foreach (var knowledge in relevantKnowledge)
            {
                await _knowledgeRepository.IncrementUsageCountAsync(knowledge.Id, cancellationToken);
            }

            var response = new AIResponse
            {
                Message = aiResponse.Data,
                ConversationId = request.ConversationId,
                Intent = DetermineIntent(request.Message),
                Confidence = CalculateConfidence(relevantKnowledge),
                UsedKnowledge = relevantKnowledge.Select(k => k.Topic).ToList(),
                SuggestedActions = GenerateSuggestedActions(request.Message, aiResponse.Data),
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation(
                "Generated AI response for conversation {ConversationId}, Intent: {Intent}, Confidence: {Confidence}",
                request.ConversationId,
                response.Intent,
                response.Confidence
            );

            return Result<AIResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate AI response for conversation {ConversationId}", request.ConversationId);
            return Result<AIResponse>.Failure($"AI response generation failed: {ex.Message}");
        }
    }

    public async Task<Result<AIConversationContext>> GetOrCreateContextAsync(
        string conversationId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var existingContext = await _contextRepository.GetByConversationIdAsync(
                conversationId,
                cancellationToken
            );

            if (existingContext != null && existingContext.IsActive && existingContext.ExpiresAt > DateTime.UtcNow)
            {
                return Result<AIConversationContext>.Success(existingContext);
            }

            // Create new context
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

            var newContext = new AIConversationContext
            {
                ConversationId = conversationId,
                UserId = userId,
                CurrentIntent = "Unknown",
                LastInteractionAt = DateTime.UtcNow,
                IsActive = true,
                ExpiresAt = DateTime.UtcNow.AddHours(24) // 24-hour context window
            };

            if (user != null)
            {
                // Add user/business context
                newContext.CustomerInfo = JsonSerializer.Serialize(new
                {
                    BusinessName = user.BusinessName,
                    Industry = user.Industry.ToString()
                });
            }

            await _contextRepository.AddAsync(newContext, cancellationToken);

            return Result<AIConversationContext>.Success(newContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get or create context for conversation {ConversationId}", conversationId);
            return Result<AIConversationContext>.Failure($"Context management failed: {ex.Message}");
        }
    }

    public async Task<Result<bool>> UpdateContextAsync(
        string contextId,
        UpdateContextRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var context = await _contextRepository.GetByIdAsync(contextId, cancellationToken);
            if (context == null)
            {
                return Result<bool>.Failure("Context not found");
            }

            if (request.CurrentIntent != null) context.CurrentIntent = request.CurrentIntent;
            if (request.CurrentTopic != null) context.CurrentTopic = request.CurrentTopic;
            if (request.CurrentIssue != null) context.CurrentIssue = request.CurrentIssue;
            if (request.PhotoAnalysis != null) context.PhotoAnalysis = request.PhotoAnalysis;

            _contextRepository.Update(context);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update context {ContextId}", contextId);
            return Result<bool>.Failure($"Context update failed: {ex.Message}");
        }
    }

    public async Task<Result<List<ProductKnowledge>>> SearchKnowledgeAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var results = await _knowledgeRepository.SearchAsync(query, cancellationToken);

            // Sort by relevance and importance
            var sortedResults = results
                .Where(k => k.IsActive && k.IsCurrentVersion)
                .OrderByDescending(k => k.IsCommonQuestion)
                .ThenByDescending(k => k.Importance)
                .ThenByDescending(k => k.TimesReferenced)
                .ToList();

            return Result<List<ProductKnowledge>>.Success(sortedResults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search knowledge base");
            return Result<List<ProductKnowledge>>.Failure($"Knowledge search failed: {ex.Message}");
        }
    }

    public async Task<Result<AITrainingExample>> RecordTrainingExampleAsync(
        RecordTrainingRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var example = new AITrainingExample
            {
                UserMessage = request.UserMessage,
                ExpectedResponse = request.ExpectedResponse,
                ActualResponse = request.ActualResponse,
                Category = request.Category,
                Intent = request.Intent,
                WasSuccessful = request.WasSuccessful,
                CustomerSatisfactionScore = request.SatisfactionScore,
                FeedbackNotes = request.FeedbackNotes,
                ConversationContext = request.ConversationContext,
                BusinessContext = request.BusinessContext,
                IncludeInTraining = true,
                ImportanceWeight = request.WasSuccessful ? 2 : 1
            };

            await _trainingRepository.AddAsync(example, cancellationToken);

            _logger.LogInformation(
                "Recorded training example: {Category}/{Intent}, Success: {Success}",
                example.Category,
                example.Intent,
                example.WasSuccessful
            );

            return Result<AITrainingExample>.Success(example);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record training example");
            return Result<AITrainingExample>.Failure($"Failed to record training: {ex.Message}");
        }
    }

    private string BuildSystemPrompt(
        List<ProductKnowledge> relevantKnowledge,
        AIConversationContext context)
    {
        var promptParts = new List<string>
        {
            SYSTEM_PROMPT,
            "\n\n**Relevant Product Knowledge:**"
        };

        foreach (var knowledge in relevantKnowledge)
        {
            promptParts.Add($"\n- {knowledge.Question}");
            promptParts.Add($"  {knowledge.ShortAnswer ?? knowledge.Answer}");
        }

        promptParts.Add("\n\n**Instructions:**");
        promptParts.Add("Provide a helpful, accurate response based on the product knowledge above.");
        promptParts.Add("Keep responses concise but complete.");
        promptParts.Add("If you don't know something, be honest and suggest contacting support.");

        return string.Join("\n", promptParts);
    }

    private string BuildUserPrompt(
        AIRequest request,
        AIConversationContext context)
    {
        var promptParts = new List<string>();

        if (!string.IsNullOrEmpty(context.LastUserMessage))
        {
            promptParts.Add("**Previous User Message:**");
            promptParts.Add(context.LastUserMessage);
            promptParts.Add("");
        }

        if (!string.IsNullOrEmpty(context.LastAIResponse))
        {
            promptParts.Add("**Previous AI Response:**");
            promptParts.Add(context.LastAIResponse);
            promptParts.Add("");
        }

        if (!string.IsNullOrEmpty(context.CurrentIssue))
        {
            promptParts.Add("**Current Issue:**");
            promptParts.Add(context.CurrentIssue);
            promptParts.Add("");
        }

        promptParts.Add("**Current User Message:**");
        promptParts.Add(request.Message);

        return string.Join("\n", promptParts);
    }

    private string DetermineIntent(string message)
    {
        var lowerMessage = message.ToLower();

        if (lowerMessage.Contains("book") || lowerMessage.Contains("schedule") || lowerMessage.Contains("appointment"))
            return "BookAppointment";

        if (lowerMessage.Contains("quote") || lowerMessage.Contains("cost") || lowerMessage.Contains("price"))
            return "GetQuote";

        if (lowerMessage.Contains("how") || lowerMessage.Contains("what") || lowerMessage.Contains("why"))
            return "AskQuestion";

        if (lowerMessage.Contains("help") || lowerMessage.Contains("support"))
            return "GetHelp";

        if (lowerMessage.Contains("cancel") || lowerMessage.Contains("reschedule"))
            return "ModifyAppointment";

        return "General";
    }

    private double CalculateConfidence(List<ProductKnowledge> knowledgeUsed)
    {
        if (!knowledgeUsed.Any())
            return 0.5; // Medium confidence without knowledge

        var avgConfidence = knowledgeUsed.Average(k => k.ConfidenceThreshold);
        var importanceBoost = knowledgeUsed.Any(k => k.IsCommonQuestion) ? 0.1 : 0;

        return Math.Min(1.0, avgConfidence + importanceBoost);
    }

    private List<string> GenerateSuggestedActions(string userMessage, string aiResponse)
    {
        var actions = new List<string>();
        var lowerMessage = userMessage.ToLower();
        var lowerResponse = aiResponse.ToLower();

        if (lowerMessage.Contains("quote") || lowerResponse.Contains("quote"))
        {
            actions.Add("Send photo for instant quote");
            actions.Add("Schedule on-site estimate");
        }

        if (lowerMessage.Contains("appointment") || lowerMessage.Contains("schedule"))
        {
            actions.Add("View available times");
            actions.Add("Confirm appointment");
        }

        if (lowerMessage.Contains("issue") || lowerMessage.Contains("problem"))
        {
            actions.Add("Send photo of the issue");
            actions.Add("Describe symptoms");
        }

        if (actions.Count == 0)
        {
            actions.Add("Ask another question");
            actions.Add("Talk to human agent");
        }

        return actions;
    }
}

#pragma warning restore CS0618
