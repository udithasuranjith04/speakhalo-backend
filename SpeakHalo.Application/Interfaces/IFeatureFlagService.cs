using SpeakHalo.Application.DTOs;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Interfaces;

public interface IFeatureFlagService
{
    Task<Result<FeatureFlagResponse>> CreateFeatureFlagAsync(CreateFeatureFlagRequest request, CancellationToken cancellationToken = default);
    Task<Result<FeatureFlagResponse>> GetFeatureFlagByIdAsync(string featureFlagId, CancellationToken cancellationToken = default);
    Task<Result<FeatureFlagResponse>> GetFeatureFlagByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Result<List<FeatureFlagResponse>>> GetAllFeatureFlagsAsync(CancellationToken cancellationToken = default);
    Task<Result<FeatureFlagResponse>> UpdateFeatureFlagAsync(string featureFlagId, UpdateFeatureFlagRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteFeatureFlagAsync(string featureFlagId, CancellationToken cancellationToken = default);
    Task<Result<bool>> IsFeatureEnabledAsync(string featureName, string userId, CancellationToken cancellationToken = default);
}
