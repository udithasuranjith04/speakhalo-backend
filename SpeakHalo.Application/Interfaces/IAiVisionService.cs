using SpeakHalo.Application.DTOs;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Interfaces;

/// <summary>
/// AI Vision service for analyzing equipment photos using Gemini Vision
/// </summary>
public interface IAiVisionService
{
    /// <summary>
    /// Analyze a single image with Gemini Vision
    /// </summary>
    Task<Result<VisionAnalysisResult>> AnalyzeImageAsync(
        VisionAnalysisRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyze multiple images and combine results
    /// </summary>
    Task<Result<MultiImageAnalysisResult>> AnalyzeMultipleImagesAsync(
        VisionAnalysisRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extract text from image using OCR (for serial numbers, model numbers)
    /// </summary>
    Task<Result<string>> ExtractTextFromImageAsync(
        byte[] imageData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate equipment identification from photo
    /// </summary>
    Task<Result<EquipmentIdentification>> IdentifyEquipmentAsync(
        byte[] imageData,
        string industry,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Diagnose issue from equipment photo
    /// </summary>
    Task<Result<IssueDiagnosis>> DiagnoseIssueAsync(
        byte[] imageData,
        string industry,
        string? customerDescription,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Recommend parts based on visual analysis
    /// </summary>
    Task<Result<List<RecommendedPart>>> RecommendPartsAsync(
        EquipmentIdentification equipment,
        IssueDiagnosis diagnosis,
        string industry,
        CancellationToken cancellationToken = default);
}
