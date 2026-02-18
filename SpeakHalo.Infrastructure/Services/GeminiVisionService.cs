using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpeakHalo.Application.AI;
using SpeakHalo.Application.DTOs;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Enums;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Infrastructure.Services;

public class GeminiVisionService : IAiVisionService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiVisionService> _logger;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly int _maxTokens;
    private readonly double _temperature;

    public GeminiVisionService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiVisionService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        _apiKey = configuration["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("Gemini API key not configured");
        _model = configuration["Gemini:VisionModel"] ?? "gemini-2.0-flash";
        _temperature = configuration.GetValue<double>("Gemini:Temperature", 0.2);
        _maxTokens = configuration.GetValue<int>("Gemini:MaxTokens", 1500);

        _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/");
        _httpClient.DefaultRequestHeaders.Add("x-goog-api-key", _apiKey);
    }

    public async Task<Result<VisionAnalysisResult>> AnalyzeImageAsync(
        VisionAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.ImageData.Count == 0 && request.ImagePaths.Count == 0)
            {
                return Result<VisionAnalysisResult>.Failure("No images provided for analysis");
            }

            byte[] imageData;
            if (request.ImageData.Count > 0)
            {
                imageData = request.ImageData[0];
            }
            else
            {
                imageData = await File.ReadAllBytesAsync(request.ImagePaths[0], cancellationToken);
            }

            var base64Image = Convert.ToBase64String(imageData);

            if (!Enum.TryParse<ServiceType>(request.Industry, true, out var industry))
            {
                industry = ServiceType.Cleaning;
            }

            var systemPrompt = IndustryVisionPrompts.GetSystemPrompt(industry);
            var analysisPrompt = IndustryVisionPrompts.GetAnalysisPrompt(
                request.AnalysisType,
                request.CustomerDescription);

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new object[]
                        {
                            new { text = analysisPrompt },
                            new
                            {
                                inlineData = new
                                {
                                    mimeType = "image/jpeg",
                                    data = base64Image
                                }
                            }
                        }
                    }
                },
                systemInstruction = new
                {
                    parts = new[] { new { text = systemPrompt } }
                },
                generationConfig = new
                {
                    temperature = _temperature,
                    maxOutputTokens = _maxTokens
                }
            };

            _logger.LogInformation(
                "Analyzing image with Gemini Vision. Type: {AnalysisType}, Industry: {Industry}",
                request.AnalysisType, request.Industry);

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                $"models/{_model}:generateContent",
                content,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Gemini API error: {StatusCode} - {Error}",
                    response.StatusCode, error);
                return Result<VisionAnalysisResult>.Failure(
                    $"Gemini API error: {response.StatusCode}");
            }

            var responseData = await response.Content.ReadAsStringAsync(cancellationToken);
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseData);

            var responseText = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            if (string.IsNullOrEmpty(responseText))
            {
                return Result<VisionAnalysisResult>.Failure("No response from Gemini Vision");
            }

            var result = ParseVisionResponse(responseText, request.AnalysisType);

            _logger.LogInformation(
                "Vision analysis complete. Confidence: {Confidence}%, Success: {Success}",
                result.ConfidenceScore, result.Success);

            return Result<VisionAnalysisResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing image with Gemini Vision");
            return Result<VisionAnalysisResult>.Failure($"Vision analysis failed: {ex.Message}");
        }
    }

    public async Task<Result<MultiImageAnalysisResult>> AnalyzeMultipleImagesAsync(
        VisionAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var individualResults = new List<VisionAnalysisResult>();

            for (int i = 0; i < Math.Min(request.ImageData.Count, 3); i++)
            {
                var singleRequest = new VisionAnalysisRequest
                {
                    ImageData = new List<byte[]> { request.ImageData[i] },
                    AnalysisType = request.AnalysisType,
                    Industry = request.Industry,
                    CustomerDescription = request.CustomerDescription,
                    IncludePartRecommendations = request.IncludePartRecommendations,
                    UseHighDetailMode = request.UseHighDetailMode
                };

                var result = await AnalyzeImageAsync(singleRequest, cancellationToken);
                if (result.IsSuccess && result.Data != null)
                {
                    individualResults.Add(result.Data);
                }
            }

            var combinedAnalysis = CombineAnalysisResults(individualResults);
            var missingAngles = DetermineMissingAngles(individualResults);
            var sufficientData = combinedAnalysis.ConfidenceScore >= 80;

            var multiResult = new MultiImageAnalysisResult
            {
                IndividualResults = individualResults,
                CombinedAnalysis = combinedAnalysis,
                TotalImagesAnalyzed = individualResults.Count,
                MissingAngles = missingAngles,
                SufficientDataForQuote = sufficientData
            };

            return Result<MultiImageAnalysisResult>.Success(multiResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing multiple images");
            return Result<MultiImageAnalysisResult>.Failure(
                $"Multi-image analysis failed: {ex.Message}");
        }
    }

    public async Task<Result<string>> ExtractTextFromImageAsync(
        byte[] imageData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var base64Image = Convert.ToBase64String(imageData);

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new object[]
                        {
                            new { text = "Extract ALL visible text from this image, especially model numbers, serial numbers, and part numbers. Format as a list." },
                            new
                            {
                                inlineData = new
                                {
                                    mimeType = "image/jpeg",
                                    data = base64Image
                                }
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.0,
                    maxOutputTokens = 500
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                $"models/{_model}:generateContent",
                content,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return Result<string>.Failure("Text extraction failed");
            }

            var responseData = await response.Content.ReadAsStringAsync(cancellationToken);
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseData);

            var text = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "";

            return Result<string>.Success(text);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from image");
            return Result<string>.Failure($"OCR failed: {ex.Message}");
        }
    }

    public async Task<Result<EquipmentIdentification>> IdentifyEquipmentAsync(
        byte[] imageData,
        string industry,
        CancellationToken cancellationToken = default)
    {
        var request = new VisionAnalysisRequest
        {
            ImageData = new List<byte[]> { imageData },
            AnalysisType = "equipment_identification",
            Industry = industry,
            UseHighDetailMode = true
        };

        var result = await AnalyzeImageAsync(request, cancellationToken);

        if (!result.IsSuccess || result.Data?.Equipment == null)
        {
            return Result<EquipmentIdentification>.Failure("Equipment identification failed");
        }

        return Result<EquipmentIdentification>.Success(result.Data.Equipment);
    }

    public async Task<Result<IssueDiagnosis>> DiagnoseIssueAsync(
        byte[] imageData,
        string industry,
        string? customerDescription,
        CancellationToken cancellationToken = default)
    {
        var request = new VisionAnalysisRequest
        {
            ImageData = new List<byte[]> { imageData },
            AnalysisType = "equipment_diagnosis",
            Industry = industry,
            CustomerDescription = customerDescription,
            UseHighDetailMode = true
        };

        var result = await AnalyzeImageAsync(request, cancellationToken);

        if (!result.IsSuccess || result.Data?.Diagnosis == null)
        {
            return Result<IssueDiagnosis>.Failure("Issue diagnosis failed");
        }

        return Result<IssueDiagnosis>.Success(result.Data.Diagnosis);
    }

    public async Task<Result<List<RecommendedPart>>> RecommendPartsAsync(
        EquipmentIdentification equipment,
        IssueDiagnosis diagnosis,
        string industry,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        return Result<List<RecommendedPart>>.Success(new List<RecommendedPart>());
    }

    private VisionAnalysisResult ParseVisionResponse(string content, string analysisType)
    {
        try
        {
            var jsonStart = content.IndexOf('{');
            var jsonEnd = content.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonContent = content.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var parsed = JsonSerializer.Deserialize<VisionAnalysisResponse>(jsonContent, options);

                if (parsed != null)
                {
                    return new VisionAnalysisResult
                    {
                        Success = true,
                        ConfidenceScore = parsed.ConfidenceScore,
                        AnalysisType = analysisType,
                        Equipment = parsed.Equipment,
                        Diagnosis = parsed.Diagnosis,
                        RecommendedParts = parsed.RecommendedParts ?? new List<RecommendedPart>(),
                        RawAnalysis = content,
                        Warnings = parsed.Warnings ?? new List<string>()
                    };
                }
            }

            return new VisionAnalysisResult
            {
                Success = true,
                ConfidenceScore = 50,
                AnalysisType = analysisType,
                RawAnalysis = content,
                Warnings = new List<string> { "Response was not in expected JSON format" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse vision response as JSON");
            return new VisionAnalysisResult
            {
                Success = false,
                ConfidenceScore = 0,
                AnalysisType = analysisType,
                RawAnalysis = content,
                ErrorMessage = $"Parse error: {ex.Message}"
            };
        }
    }

    private VisionAnalysisResult CombineAnalysisResults(List<VisionAnalysisResult> results)
    {
        if (results.Count == 0)
        {
            return new VisionAnalysisResult { Success = false, ConfidenceScore = 0 };
        }

        if (results.Count == 1)
        {
            return results[0];
        }

        var avgConfidence = results.Average(r => r.ConfidenceScore);
        var allParts = results.SelectMany(r => r.RecommendedParts).DistinctBy(p => p.PartNumber).ToList();
        var equipment = results.FirstOrDefault(r => r.Equipment != null)?.Equipment;
        var diagnosis = results.FirstOrDefault(r => r.Diagnosis != null)?.Diagnosis;

        return new VisionAnalysisResult
        {
            Success = true,
            ConfidenceScore = avgConfidence,
            AnalysisType = results[0].AnalysisType,
            Equipment = equipment,
            Diagnosis = diagnosis,
            RecommendedParts = allParts,
            RawAnalysis = string.Join("\n\n", results.Select(r => r.RawAnalysis))
        };
    }

    private List<string> DetermineMissingAngles(List<VisionAnalysisResult> results)
    {
        var missing = new List<string>();

        if (results.Average(r => r.ConfidenceScore) < 70)
        {
            missing.Add("Close-up of model/serial plate");
            missing.Add("Overall unit view");
            missing.Add("Side view showing connections");
        }

        return missing;
    }

    private class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<Candidate>? Candidates { get; set; }
    }

    private class Candidate
    {
        [JsonPropertyName("content")]
        public ContentBlock? Content { get; set; }
    }

    private class ContentBlock
    {
        [JsonPropertyName("parts")]
        public List<Part>? Parts { get; set; }
    }

    private class Part
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    private class VisionAnalysisResponse
    {
        [JsonPropertyName("equipment")]
        public EquipmentIdentification? Equipment { get; set; }

        [JsonPropertyName("diagnosis")]
        public IssueDiagnosis? Diagnosis { get; set; }

        [JsonPropertyName("recommendedParts")]
        public List<RecommendedPart>? RecommendedParts { get; set; }

        [JsonPropertyName("confidenceScore")]
        public double ConfidenceScore { get; set; }

        [JsonPropertyName("warnings")]
        public List<string>? Warnings { get; set; }
    }
}
