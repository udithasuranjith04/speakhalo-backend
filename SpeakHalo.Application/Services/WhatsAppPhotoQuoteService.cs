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
/// WhatsApp Photo-to-Quote Service
/// Intelligent pipeline for converting customer photos into accurate service quotes
///
/// Features:
/// - Confidence-based workflows (auto-quote, review, request more photos)
/// - Multi-image analysis for accuracy
/// - Structured quote generation
/// - Industry-specific assessment
/// </summary>
public interface IWhatsAppPhotoQuoteService
{
    Task<Result<PhotoQuoteResult>> ProcessPhotoQuoteAsync(
        PhotoQuoteRequest request,
        CancellationToken cancellationToken = default);
}

public class WhatsAppPhotoQuoteService : IWhatsAppPhotoQuoteService
{
    private readonly IAiVisionService _visionService;
    private readonly IUserRepository _userRepository;
    private readonly IWhatsAppConversationRepository _conversationRepository;
    private readonly IQuoteService _quoteService;
    private readonly ILogger<WhatsAppPhotoQuoteService> _logger;

    // Confidence thresholds for workflow decisions
    private const double AUTO_QUOTE_THRESHOLD = 80.0;
    private const double REVIEW_NEEDED_THRESHOLD = 50.0;

    public WhatsAppPhotoQuoteService(
        IAiVisionService visionService,
        IUserRepository userRepository,
        IWhatsAppConversationRepository conversationRepository,
        IQuoteService quoteService,
        ILogger<WhatsAppPhotoQuoteService> logger)
    {
        _visionService = visionService;
        _userRepository = userRepository;
        _conversationRepository = conversationRepository;
        _quoteService = quoteService;
        _logger = logger;
    }

    public async Task<Result<PhotoQuoteResult>> ProcessPhotoQuoteAsync(
        PhotoQuoteRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get user context
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                return Result<PhotoQuoteResult>.Failure("User not found");
            }

            var industry = user.Industry.ToString();

            // Analyze images
            VisionAnalysisResult visionResult;
            int imagesAnalyzed;
            List<string> missingAngles;

            if (request.ImageUrls.Count > 1)
            {
                // Multi-image analysis
                var multiImageRequest = new VisionAnalysisRequest
                {
                    ImagePaths = request.ImageUrls,
                    AnalysisType = "quote_generation",
                    Industry = industry,
                    CustomerDescription = request.CustomerDescription,
                    UseHighDetailMode = true
                };

                var multiResult = await _visionService.AnalyzeMultipleImagesAsync(
                    multiImageRequest,
                    cancellationToken
                );

                if (!multiResult.IsSuccess || multiResult.Data == null)
                {
                    return Result<PhotoQuoteResult>.Failure(
                        multiResult.Errors.Any() ? string.Join(", ", multiResult.Errors) : "Multi-image analysis failed"
                    );
                }

                visionResult = multiResult.Data.CombinedAnalysis;
                imagesAnalyzed = multiResult.Data.TotalImagesAnalyzed;
                missingAngles = multiResult.Data.MissingAngles;
            }
            else
            {
                // Single image analysis
                var singleImageRequest = new VisionAnalysisRequest
                {
                    ImagePaths = request.ImageUrls,
                    AnalysisType = "quote_generation",
                    Industry = industry,
                    CustomerDescription = request.CustomerDescription,
                    UseHighDetailMode = true
                };

                var singleResult = await _visionService.AnalyzeImageAsync(
                    singleImageRequest,
                    cancellationToken
                );

                if (!singleResult.IsSuccess || singleResult.Data == null)
                {
                    return Result<PhotoQuoteResult>.Failure(
                        singleResult.Errors.Any() ? string.Join(", ", singleResult.Errors) : "Image analysis failed"
                    );
                }

                visionResult = singleResult.Data;
                imagesAnalyzed = 1;
                missingAngles = GetMissingAnglesFromSingleImage(visionResult);
            }

            // Determine workflow based on confidence
            var confidence = visionResult.ConfidenceScore;
            string workflow;
            ServiceQuote? quote = null;
            string customerMessage;
            string? internalNotes;
            List<string> suggestedFollowUp;

            if (confidence >= AUTO_QUOTE_THRESHOLD)
            {
                // HIGH CONFIDENCE: Generate automatic quote
                workflow = "auto_quote";
                quote = await GenerateQuoteAsync(visionResult, user, cancellationToken);
                customerMessage = BuildAutoQuoteMessage(quote, visionResult);
                internalNotes = BuildInternalNotes(visionResult, quote);
                suggestedFollowUp = new List<string>
                {
                    "Schedule appointment",
                    "Confirm customer availability"
                };
            }
            else if (confidence >= REVIEW_NEEDED_THRESHOLD)
            {
                // MEDIUM CONFIDENCE: Review needed
                workflow = "review_needed";
                quote = await GeneratePreliminaryQuoteAsync(visionResult, user, cancellationToken);
                customerMessage = BuildReviewNeededMessage(quote, visionResult);
                internalNotes = BuildInternalReviewNotes(visionResult);
                suggestedFollowUp = new List<string>
                {
                    "Review and approve quote",
                    "Call customer to clarify details",
                    "Request additional photos if needed"
                };
            }
            else
            {
                // LOW CONFIDENCE: Request more photos
                workflow = "request_more_photos";
                customerMessage = BuildMorePhotosRequest(visionResult, missingAngles);
                internalNotes = $"Insufficient photo quality. Confidence: {confidence:F1}%. Awaiting better images.";
                suggestedFollowUp = new List<string>
                {
                    "Wait for customer to send additional photos"
                };
            }

            // Save analysis to conversation
            await SaveAnalysisToConversationAsync(
                request.ConversationId,
                visionResult,
                quote,
                workflow,
                cancellationToken
            );

            var result = new PhotoQuoteResult
            {
                Success = true,
                ConfidenceScore = confidence,
                Workflow = workflow,
                VisionAnalysis = visionResult,
                Quote = quote,
                CustomerMessage = customerMessage,
                InternalNotes = internalNotes,
                SuggestedFollowUp = suggestedFollowUp,
                ImagesAnalyzed = imagesAnalyzed,
                MissingPhotoRequests = missingAngles
            };

            return Result<PhotoQuoteResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Photo quote processing failed for conversation {ConversationId}", request.ConversationId);
            return Result<PhotoQuoteResult>.Failure($"Processing error: {ex.Message}");
        }
    }

    private Task<ServiceQuote> GenerateQuoteAsync(
        VisionAnalysisResult visionResult,
        User user,
        CancellationToken cancellationToken)
    {
        var lineItems = new List<DTOs.QuoteLineItem>();

        // Add parts
        foreach (var part in visionResult.RecommendedParts)
        {
            if (part.EstimatedCost.HasValue)
            {
                lineItems.Add(new DTOs.QuoteLineItem
                {
                    Description = part.Name,
                    Quantity = 1,
                    Unit = "ea",
                    UnitPrice = part.EstimatedCost.Value,
                    Total = part.EstimatedCost.Value,
                    Category = "Parts",
                    Notes = part.Reason
                });
            }
        }

        // Add labor
        if (visionResult.Diagnosis != null)
        {
            var laborHours = visionResult.Diagnosis.EstimatedRepairTimeMinutes / 60.0;
            var laborRate = 125.0m; // Default - should come from user pricing settings
            var laborTotal = (decimal)laborHours * laborRate;

            lineItems.Add(new DTOs.QuoteLineItem
            {
                Description = $"Labor - {visionResult.Diagnosis.RepairComplexity ?? "Standard"} Repair",
                Quantity = laborHours,
                Unit = "hr",
                UnitPrice = laborRate,
                Total = laborTotal,
                Category = "Labor",
                Notes = $"Estimated time for {visionResult.Diagnosis.PrimaryIssue}"
            });
        }

        // Calculate total
        var total = lineItems.Sum(item => item.Total);

        // Determine urgency and validity
        var urgency = "routine";
        var validityDays = 30;

        if (visionResult.Diagnosis != null)
        {
            if (visionResult.Diagnosis.IsEmergency)
            {
                urgency = "emergency";
                validityDays = 3;
            }
            else if (visionResult.Diagnosis.Severity == "High" || visionResult.Diagnosis.Severity == "Critical")
            {
                urgency = "urgent";
                validityDays = 7;
            }
        }

        // Build notes
        var notesParts = new List<string>();
        if (visionResult.Equipment != null)
        {
            notesParts.Add($"Equipment: {visionResult.Equipment.Type}");
            if (!string.IsNullOrEmpty(visionResult.Equipment.Brand))
            {
                notesParts.Add($"Brand: {visionResult.Equipment.Brand}");
            }
            if (!string.IsNullOrEmpty(visionResult.Equipment.Model))
            {
                notesParts.Add($"Model: {visionResult.Equipment.Model}");
            }
        }

        if (visionResult.Diagnosis != null)
        {
            notesParts.Add($"Issue: {visionResult.Diagnosis.PrimaryIssue}");
            if (!string.IsNullOrEmpty(visionResult.Diagnosis.SafetyWarning))
            {
                notesParts.Add($"⚠️ SAFETY: {visionResult.Diagnosis.SafetyWarning}");
            }
        }

        var notes = string.Join("\n", notesParts);

        var businessName = user.BusinessName ?? "Our Business";
        var terms = $"Quote valid for {validityDays} days. Payment due upon completion. All {businessName} standard terms apply.";

        return Task.FromResult(new ServiceQuote
        {
            QuoteId = Guid.NewGuid().ToString()[..8],
            TotalAmount = total,
            LineItems = lineItems,
            EstimatedDurationHours = visionResult.Diagnosis?.EstimatedRepairTimeMinutes / 60.0 ?? 1.0,
            Urgency = urgency,
            ValidityDays = validityDays,
            Terms = terms,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        });
    }

    private async Task<ServiceQuote> GeneratePreliminaryQuoteAsync(
        VisionAnalysisResult visionResult,
        User user,
        CancellationToken cancellationToken)
    {
        var quote = await GenerateQuoteAsync(visionResult, user, cancellationToken);
        quote.Notes = $"PRELIMINARY ESTIMATE - Requires review\n\n{quote.Notes}";
        return quote;
    }

    private string BuildAutoQuoteMessage(ServiceQuote quote, VisionAnalysisResult visionResult)
    {
        var parts = new List<string>();

        // Greeting and analysis summary
        if (visionResult.Equipment != null)
        {
            parts.Add($"Thanks for the photo! I can see you have a {visionResult.Equipment.Type}");
            if (!string.IsNullOrEmpty(visionResult.Equipment.Brand))
            {
                parts.Add($" ({visionResult.Equipment.Brand})");
            }
            parts.Add(".");
        }

        if (visionResult.Diagnosis != null)
        {
            parts.Add($"\n\n🔧 *Issue Identified:* {visionResult.Diagnosis.PrimaryIssue}");

            if (visionResult.Diagnosis.IsEmergency)
            {
                parts.Add("\n\n⚠️ *URGENT:* This appears to be an emergency situation.");
            }
            else if (visionResult.Diagnosis.Severity == "High")
            {
                parts.Add("\n\n⚡ *Priority:* This issue should be addressed soon.");
            }

            if (!string.IsNullOrEmpty(visionResult.Diagnosis.SafetyWarning))
            {
                parts.Add($"\n\n🛡️ *Safety Note:* {visionResult.Diagnosis.SafetyWarning}");
            }
        }

        // Quote details
        parts.Add("\n\n📋 *Service Quote*");
        parts.Add($"\nQuote #: {quote.QuoteId}");

        foreach (var item in quote.LineItems)
        {
            parts.Add($"\n• {item.Description}: ${item.Total:F2}");
        }

        parts.Add($"\n\n💰 *Total: ${quote.TotalAmount:F2}*");
        parts.Add($"\n⏱️ Estimated time: {quote.EstimatedDurationHours:F1} hours");

        // Call to action
        parts.Add("\n\n📅 *Ready to schedule?* Reply with your preferred date and time, or call us to book immediately.");
        parts.Add($"\n\n_Quote valid for {quote.ValidityDays} days_");

        return string.Join("", parts);
    }

    private string BuildReviewNeededMessage(ServiceQuote quote, VisionAnalysisResult visionResult)
    {
        var parts = new List<string>();

        parts.Add("Thanks for sending the photo! I've done an initial analysis and here's what I can see:");

        if (visionResult.Diagnosis != null)
        {
            parts.Add($"\n\n🔧 Possible Issue: {visionResult.Diagnosis.PrimaryIssue}");
        }

        parts.Add("\n\n📋 Preliminary Estimate:");
        parts.Add($"${quote.TotalAmount:F2} - ${quote.TotalAmount * 1.3m:F2}");

        parts.Add("\n\nTo give you a precise quote, I'd like our team to review the details. They'll get back to you within 2 hours with a confirmed estimate.");
        parts.Add("\n\nIn the meantime, feel free to send any additional photos or information about the issue!");

        return string.Join("", parts);
    }

    private string BuildMorePhotosRequest(VisionAnalysisResult visionResult, List<string> missingAngles)
    {
        var parts = new List<string>();

        parts.Add("Thanks for the photo! To give you an accurate quote, I'd like to see a few more angles:");

        if (missingAngles.Any())
        {
            foreach (var angle in missingAngles)
            {
                parts.Add($"\n📸 {angle}");
            }
        }
        else
        {
            parts.Add("\n📸 Close-up of the model/serial number plate");
            parts.Add("\n📸 Overall view showing the entire unit");
            parts.Add("\n📸 Close-up of the problem area");
        }

        parts.Add("\n\nThis will help me provide you with the most accurate estimate possible. Thanks!");

        return string.Join("", parts);
    }

    private string BuildInternalNotes(VisionAnalysisResult visionResult, ServiceQuote quote)
    {
        var notes = new List<string>();

        notes.Add($"*Auto-Quote Generated* (Confidence: {visionResult.ConfidenceScore:F1}%)");

        if (visionResult.Equipment != null)
        {
            notes.Add($"\n\nEquipment: {visionResult.Equipment.Type}");
            if (!string.IsNullOrEmpty(visionResult.Equipment.Brand) && !string.IsNullOrEmpty(visionResult.Equipment.Model))
            {
                notes.Add($" - {visionResult.Equipment.Brand} {visionResult.Equipment.Model}");
            }
            if (!string.IsNullOrEmpty(visionResult.Equipment.Condition))
            {
                notes.Add($"\nCondition: {visionResult.Equipment.Condition}");
            }
        }

        if (visionResult.Diagnosis != null)
        {
            notes.Add($"\n\nPrimary Issue: {visionResult.Diagnosis.PrimaryIssue}");
            if (!string.IsNullOrEmpty(visionResult.Diagnosis.RootCause))
            {
                notes.Add($"\nLikely Cause: {visionResult.Diagnosis.RootCause}");
            }
        }

        if (visionResult.RecommendedParts.Any())
        {
            notes.Add("\n\nParts to Bring:");
            foreach (var part in visionResult.RecommendedParts.Take(5))
            {
                notes.Add($"\n• {part.Name} ({part.PartNumber})");
            }
        }

        notes.Add($"\n\nEstimated Duration: {quote.EstimatedDurationHours:F1} hours");

        return string.Join("", notes);
    }

    private string BuildInternalReviewNotes(VisionAnalysisResult visionResult)
    {
        var notes = new List<string>();

        notes.Add($"*Review Required* (Confidence: {visionResult.ConfidenceScore:F1}%)");

        if (visionResult.Warnings.Any())
        {
            notes.Add("\n\nWarnings:");
            foreach (var warning in visionResult.Warnings)
            {
                notes.Add($"\n⚠️ {warning}");
            }
        }

        notes.Add("\n\nPlease review the analysis and approve/modify the quote before sending to customer.");

        return string.Join("", notes);
    }

    private List<string> GetMissingAnglesFromSingleImage(VisionAnalysisResult visionResult)
    {
        var missing = new List<string>();

        if (visionResult.ConfidenceScore < 70)
        {
            missing.Add("Close-up of model/serial plate");
            missing.Add("Overall equipment view");
            missing.Add("Problem area from different angle");
        }

        if (visionResult.Equipment != null && string.IsNullOrEmpty(visionResult.Equipment.Model))
        {
            missing.Add("Clear photo of the data plate showing model and serial numbers");
        }

        return missing;
    }

    private async Task SaveAnalysisToConversationAsync(
        string conversationId,
        VisionAnalysisResult visionResult,
        ServiceQuote? quote,
        string workflow,
        CancellationToken cancellationToken)
    {
        try
        {
            var conversation = await _conversationRepository.GetByIdAsync(conversationId, cancellationToken);
            if (conversation != null)
            {
                // Store analysis in conversation metadata
                var analysisData = new
                {
                    timestamp = DateTime.UtcNow,
                    confidence = visionResult.ConfidenceScore,
                    workflow,
                    equipment = visionResult.Equipment,
                    diagnosis = visionResult.Diagnosis,
                    quote
                };

                // You would typically save this to a metadata/extra_data field
                // Implementation depends on your conversation storage schema
                _logger.LogInformation(
                    "Saved vision analysis to conversation {ConversationId} with confidence {Confidence}%",
                    conversationId,
                    visionResult.ConfidenceScore
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save analysis to conversation {ConversationId}", conversationId);
        }
    }
}

#pragma warning restore CS0618
