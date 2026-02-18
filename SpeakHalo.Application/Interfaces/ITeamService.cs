using SpeakHalo.Domain.Enums;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Interfaces;

public interface ITeamService
{
    Task<Result<TeamDto>> GetTeamAsync(string userId);
    Task<Result<TeamDto>> CreateTeamAsync(string userId, string teamName);
    Task<Result<List<TeamMemberDto>>> GetMembersAsync(string userId);
    Task<Result<TeamInviteDto>> InviteMemberAsync(string userId, string email, TeamRole role);
    Task<Result<bool>> AcceptInviteAsync(string token, string userId);
    Task<Result<bool>> DeclineInviteAsync(string token, string userId);
    Task<Result<bool>> RevokeInviteAsync(string inviteId, string userId);
    Task<Result<bool>> RemoveMemberAsync(string memberId, string userId);
    Task<Result<bool>> UpdateMemberRoleAsync(string memberId, TeamRole newRole, string userId);
    Task<Result<List<TeamInviteDto>>> GetPendingInvitesAsync(string userId);
    Task<Result<TeamInviteDto>> GetInviteByTokenAsync(string token);
}

public class TeamDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public int MaxMembers { get; set; }
    public int CurrentMembers { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TeamMemberDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}

public class TeamInviteDto
{
    public string Id { get; set; } = string.Empty;
    public string TeamId { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string InvitedBy { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
