namespace SpeakHalo.Domain.Entities;

/// <summary>
/// Admin management and tracking for users
/// </summary>
public class UserAdmin : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string? CreatedByAdminId { get; set; }
    public string? AssignedSalesRepId { get; set; }
    public string? AccountManagerNotes { get; set; }
    public string? AdminNotes { get; set; }

    // Account Status
    public string? BlockReason { get; set; }
    public DateTime? BlockedAt { get; set; }
    public string? BlockedByAdminId { get; set; }

    public virtual User User { get; set; } = null!;
}
