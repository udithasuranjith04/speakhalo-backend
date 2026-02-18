namespace SpeakHalo.Domain.Enums;

/// <summary>
/// LEGACY: Kept for DB compatibility only. No longer used in business logic.
/// Admin controls all user access and limits directly — there are no plan tiers.
/// </summary>
public enum PlanTier
{
    None = 0,
    Starter = 1,
    Professional = 2,
    Business = 3,
    Premium = 4
}
