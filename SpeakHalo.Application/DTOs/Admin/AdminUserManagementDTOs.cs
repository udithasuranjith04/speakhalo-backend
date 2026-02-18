namespace SpeakHalo.Application.DTOs.Admin;

public class AdminCreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? BusinessName { get; set; }
    public string? BusinessPhone { get; set; }
    public string? BusinessAddress { get; set; }
    public string? BusinessWebsite { get; set; }
    public int? AIMinutes { get; set; }
    public int? WhatsAppMessages { get; set; }
    public int? SMSMessages { get; set; }
    public int? Images { get; set; }
    public string? AdminNotes { get; set; }
    /// <summary>Optional pricing plan ID. If provided, plan limits are used as defaults (overridable by explicit limit values).</summary>
    public string? PricingPlanId { get; set; }
    public string? AgentName { get; set; }
    public string? VoiceName { get; set; }
    public string? Greeting { get; set; }
}

public class AdminUpdateLimitsRequest
{
    public int? AIMinutes { get; set; }
    public int? WhatsAppMessages { get; set; }
    public int? SMSMessages { get; set; }
    public int? Images { get; set; }
    public bool? EnforceHardLimits { get; set; }
}

public class AdminSuspendRequest
{
    public bool Suspend { get; set; } = true;
    public string? Reason { get; set; }
}

public class AdminUpdateProfileRequest
{
    public string? Name { get; set; }
    public string? BusinessName { get; set; }
    public string? BusinessPhone { get; set; }
    public string? BusinessAddress { get; set; }
    public string? BusinessWebsite { get; set; }
    public string? IndustrySpecialization { get; set; }
}

public class AdminUpdateFeaturesRequest
{
    public bool? VoiceAI { get; set; }
    public bool? WhatsApp { get; set; }
    public bool? SMS { get; set; }
    public bool? AIPhotoQuotes { get; set; }
    public bool? KnowledgeBase { get; set; }
    public bool? Calendar { get; set; }
    public bool? CustomBranding { get; set; }
    public bool? MultipleLocations { get; set; }
    public bool? TeamMembers { get; set; }
    public bool? AdvancedAnalytics { get; set; }
}

public class AdminUpdateAiConfigRequest
{
    public string? AgentName { get; set; }
    public string? VoiceName { get; set; }
    public string? Greeting { get; set; }
    public string? SystemPrompt { get; set; }
    public string? PersonalityTone { get; set; }
    public string? Language { get; set; }
    public List<string>? SpecialInstructions { get; set; }
    public string? BusinessHoursMessage { get; set; }
    public string? AfterHoursMessage { get; set; }
}
