using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Domain.Entities;

public class Customer : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public LeadStatus LeadStatus { get; set; } = LeadStatus.New;
    public int LeadScore { get; set; } = 0;
    public string LeadQualification { get; set; } = "cold";

    public decimal TotalSpend { get; set; } = 0;
    public int JobCount { get; set; } = 0;

    public string? Source { get; set; }
    public DateTime? ConvertedAt { get; set; }
    public string? Notes { get; set; }

    public DateTime? LastContactedAt { get; set; }
    public DateTime? LastServiceDate { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
