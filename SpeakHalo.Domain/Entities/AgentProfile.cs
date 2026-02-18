namespace SpeakHalo.Domain.Entities;

public class AgentProfile : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string GreetingMessage { get; set; } = string.Empty;
    public string ClosingMessage { get; set; } = string.Empty;
    public string ServicesList { get; set; } = string.Empty;
    public string BusinessHours { get; set; } = string.Empty;
    public string VoicemailMessage { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public string Tone { get; set; } = "professional";
    public string? HumanBackupNumber { get; set; }
    public bool TransferOnComplexIssues { get; set; } = true;
    public string? CustomInstructions { get; set; }

    public string? BrandName { get; set; }
    public string? BrandTagline { get; set; }
    public string? BrandColor { get; set; }
    public string? LogoUrl { get; set; }

    public bool EnableTroubleshooting { get; set; } = true;

    public virtual User User { get; set; } = null!;
}
