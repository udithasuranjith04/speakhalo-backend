using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Domain.Entities;

/// <summary>
/// Represents a team/organization. Created automatically when a user subscribes to a plan.
/// The plan owner is the team owner. Team members share access to the owner's resources.
/// </summary>
public class Team : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public int MaxMembers { get; set; } = 5;

    public virtual User Owner { get; set; } = null!;
    public virtual ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
    public virtual ICollection<TeamInvite> Invites { get; set; } = new List<TeamInvite>();
}

/// <summary>
/// Represents a user's membership in a team with a specific role.
/// </summary>
public class TeamMember : BaseEntity
{
    public string TeamId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public TeamRole Role { get; set; } = TeamRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public virtual Team Team { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}

/// <summary>
/// Represents an invitation to join a team. Invites expire after 7 days.
/// </summary>
public class TeamInvite : BaseEntity
{
    public string TeamId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public TeamRole Role { get; set; } = TeamRole.Member;
    public InviteStatus Status { get; set; } = InviteStatus.Pending;
    public string InvitedByUserId { get; set; } = string.Empty;
    public string? AcceptedByUserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);
    public DateTime? RespondedAt { get; set; }

    public virtual Team Team { get; set; } = null!;
    public virtual User InvitedByUser { get; set; } = null!;
}
