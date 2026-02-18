using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Application.Interfaces;

/// <summary>
/// Simplified feature gating — no plan tiers.
/// All active users get all features. Admin controls limits per user directly.
/// </summary>
public interface IFeatureGatingService
{
    bool HasFeature(PlanFeature feature);
    IReadOnlyList<PlanFeature> GetAllFeatures();
    bool IsUserActive(bool hasActivePayment, bool isTemporarilySuspended);
}
