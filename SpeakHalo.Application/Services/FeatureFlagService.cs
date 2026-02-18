using Microsoft.Extensions.Logging;
using SpeakHalo.Application.DTOs;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;
using SpeakHalo.Shared.Constants;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Services;

public class FeatureFlagService : IFeatureFlagService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<FeatureFlagService> _logger;

    public FeatureFlagService(IUnitOfWork unitOfWork, ILogger<FeatureFlagService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<FeatureFlagResponse>> CreateFeatureFlagAsync(CreateFeatureFlagRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await _unitOfWork.Repository<FeatureFlag>().FirstOrDefaultAsync(f => f.Name == request.Name, cancellationToken);
            if (existing != null)
                return Result<FeatureFlagResponse>.Failure($"Feature flag '{request.Name}' already exists");

            var featureFlag = new FeatureFlag
            {
                Name = request.Name,
                Description = request.Description,
                IsEnabled = request.IsEnabled,
                EnabledForUsers = request.EnabledForUsers,
                RolloutPercentage = request.RolloutPercentage,
                Metadata = request.Metadata
            };

            await _unitOfWork.Repository<FeatureFlag>().AddAsync(featureFlag, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<FeatureFlagResponse>.Success(MapToResponse(featureFlag));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create feature flag {Name}", request.Name);
            return Result<FeatureFlagResponse>.Failure($"Failed to create feature flag: {ex.Message}");
        }
    }

    public async Task<Result<FeatureFlagResponse>> GetFeatureFlagByIdAsync(string featureFlagId, CancellationToken cancellationToken = default)
    {
        try
        {
            var featureFlag = await _unitOfWork.Repository<FeatureFlag>().GetByIdAsync(featureFlagId, cancellationToken);
            if (featureFlag == null)
                return Result<FeatureFlagResponse>.Failure(ErrorMessages.NotFound("Feature flag"));

            return Result<FeatureFlagResponse>.Success(MapToResponse(featureFlag));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get feature flag {FlagId}", featureFlagId);
            return Result<FeatureFlagResponse>.Failure($"Failed to get feature flag: {ex.Message}");
        }
    }

    public async Task<Result<FeatureFlagResponse>> GetFeatureFlagByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            var featureFlag = await _unitOfWork.Repository<FeatureFlag>().FirstOrDefaultAsync(f => f.Name == name, cancellationToken);
            if (featureFlag == null)
                return Result<FeatureFlagResponse>.Failure(ErrorMessages.NotFound("Feature flag"));

            return Result<FeatureFlagResponse>.Success(MapToResponse(featureFlag));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get feature flag {Name}", name);
            return Result<FeatureFlagResponse>.Failure($"Failed to get feature flag: {ex.Message}");
        }
    }

    public async Task<Result<List<FeatureFlagResponse>>> GetAllFeatureFlagsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var featureFlags = await _unitOfWork.Repository<FeatureFlag>().GetAllAsync(cancellationToken);
            var responses = featureFlags.Select(MapToResponse).ToList();

            return Result<List<FeatureFlagResponse>>.Success(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all feature flags");
            return Result<List<FeatureFlagResponse>>.Failure($"Failed to get feature flags: {ex.Message}");
        }
    }

    public async Task<Result<FeatureFlagResponse>> UpdateFeatureFlagAsync(string featureFlagId, UpdateFeatureFlagRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var featureFlag = await _unitOfWork.Repository<FeatureFlag>().GetByIdAsync(featureFlagId, cancellationToken);
            if (featureFlag == null)
                return Result<FeatureFlagResponse>.Failure(ErrorMessages.NotFound("Feature flag"));

            if (request.Description != null) featureFlag.Description = request.Description;
            if (request.IsEnabled.HasValue) featureFlag.IsEnabled = request.IsEnabled.Value;
            if (request.EnabledForUsers != null) featureFlag.EnabledForUsers = request.EnabledForUsers;
            if (request.RolloutPercentage.HasValue) featureFlag.RolloutPercentage = request.RolloutPercentage.Value;
            if (request.Metadata != null) featureFlag.Metadata = request.Metadata;

            _unitOfWork.Repository<FeatureFlag>().Update(featureFlag);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<FeatureFlagResponse>.Success(MapToResponse(featureFlag));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update feature flag {FlagId}", featureFlagId);
            return Result<FeatureFlagResponse>.Failure($"Failed to update feature flag: {ex.Message}");
        }
    }

    public async Task<Result<bool>> DeleteFeatureFlagAsync(string featureFlagId, CancellationToken cancellationToken = default)
    {
        try
        {
            var featureFlag = await _unitOfWork.Repository<FeatureFlag>().GetByIdAsync(featureFlagId, cancellationToken);
            if (featureFlag == null)
                return Result<bool>.Failure(ErrorMessages.NotFound("Feature flag"));

            _unitOfWork.Repository<FeatureFlag>().Remove(featureFlag);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete feature flag {FlagId}", featureFlagId);
            return Result<bool>.Failure($"Failed to delete feature flag: {ex.Message}");
        }
    }

    public async Task<Result<bool>> IsFeatureEnabledAsync(string featureName, string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var featureFlag = await _unitOfWork.Repository<FeatureFlag>().FirstOrDefaultAsync(f => f.Name == featureName, cancellationToken);
            if (featureFlag == null)
                return Result<bool>.Success(false);

            if (!featureFlag.IsEnabled)
                return Result<bool>.Success(false);

            if (!string.IsNullOrEmpty(featureFlag.EnabledForUsers))
            {
                var allowedUsers = featureFlag.EnabledForUsers.Split(',').Select(u => u.Trim()).ToList();
                if (allowedUsers.Contains(userId))
                    return Result<bool>.Success(true);
            }

            if (featureFlag.RolloutPercentage > 0)
            {
                var hash = Math.Abs(userId.GetHashCode()) % 100;
                if (hash < featureFlag.RolloutPercentage)
                    return Result<bool>.Success(true);
            }

            if (string.IsNullOrEmpty(featureFlag.EnabledForUsers) && featureFlag.RolloutPercentage == 0)
            {
                return Result<bool>.Success(true);
            }

            return Result<bool>.Success(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check feature flag {Name} for user {UserId}", featureName, userId);
            return Result<bool>.Failure($"Failed to check feature flag: {ex.Message}");
        }
    }

    private static FeatureFlagResponse MapToResponse(FeatureFlag featureFlag)
    {
        return new FeatureFlagResponse
        {
            Id = featureFlag.Id,
            Name = featureFlag.Name,
            Description = featureFlag.Description,
            IsEnabled = featureFlag.IsEnabled,
            EnabledForUsers = featureFlag.EnabledForUsers,
            RolloutPercentage = featureFlag.RolloutPercentage,
            Metadata = featureFlag.Metadata,
            CreatedAt = featureFlag.CreatedAt,
            UpdatedAt = featureFlag.UpdatedAt
        };
    }
}
