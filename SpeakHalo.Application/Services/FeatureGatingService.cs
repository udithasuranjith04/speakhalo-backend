using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Application.Services;

/// <summary>
/// Simplified feature gating — all features available to all active users.
/// Admin controls access by activating/deactivating users and setting usage limits per user.
/// No plan tiers, no plan-based limits. Admin sets everything directly on UserSubscription.
/// </summary>
public class FeatureGatingService : IFeatureGatingService
{
    private static readonly IReadOnlyList<PlanFeature> AllFeaturesList =
        Enum.GetValues<PlanFeature>().ToList().AsReadOnly();

    /// <summary>
    /// All active users have all features. Returns true if user is active (has any non-None tier).
    /// </summary>
    public bool HasFeature(PlanFeature feature)
    {
        // All features available to all active users
        return true;
    }

    /// <summary>
    /// Returns all features — every active user gets everything.
    /// </summary>
    public IReadOnlyList<PlanFeature> GetAllFeatures()
    {
        return AllFeaturesList;
    }

    /// <summary>
    /// Check if user is active based on subscription status.
    /// </summary>
    public bool IsUserActive(bool hasActivePayment, bool isTemporarilySuspended)
    {
        return hasActivePayment && !isTemporarilySuspended;
    }
}
