namespace SpeakHalo.Domain.Enums;

/// <summary>
/// Feature identifiers used by RequireFeatureAttribute for endpoint access control.
/// LEGACY tier-based gating removed — all features are available to all active users.
/// Kept for use as feature labels in the RequireFeature attribute.
/// </summary>
public enum PlanFeature
{
    VoiceAI,
    WhatsApp,
    SMS,
    AIPhotoQuotes,
    AIFollowUps,
    AdvancedAnalytics,
    SentimentAnalysis,
    MultiplePhoneNumbers,
    PrioritySupport,
    MultipleLocations,
    BasicCRM,
    Calendar,
    CallTranscription,
    KnowledgeBase,
    CustomBranding,
    APIAccess,
    TeamMembers
}
