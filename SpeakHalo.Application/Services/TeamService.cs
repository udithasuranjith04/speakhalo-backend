using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;
using SpeakHalo.Domain.Interfaces.Repositories;
using SpeakHalo.Shared.Models;

#pragma warning disable CS0618

namespace SpeakHalo.Application.Services;

public class TeamService : ITeamService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<TeamService> _logger;

    public TeamService(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ILogger<TeamService> logger)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<TeamDto>> GetTeamAsync(string userId)
    {
        // Check if user owns a team
        var teams = await _unitOfWork.Repository<Team>()
            .FindAsync(t => t.OwnerId == userId);
        var team = teams.FirstOrDefault();

        if (team == null)
        {
            // Check if user is a member of any team
            var memberships = await _unitOfWork.Repository<TeamMember>()
                .FindAsync(m => m.UserId == userId);
            var membership = memberships.FirstOrDefault();
            if (membership == null)
                return Result<TeamDto>.Failure("You are not part of any team");

            var memberTeams = await _unitOfWork.Repository<Team>()
                .FindAsync(t => t.Id == membership.TeamId);
            team = memberTeams.FirstOrDefault();
            if (team == null)
                return Result<TeamDto>.Failure("Team not found");
        }

        var members = await _unitOfWork.Repository<TeamMember>()
            .FindAsync(m => m.TeamId == team.Id);

        var owner = await _unitOfWork.Repository<User>()
            .GetByIdAsync(team.OwnerId);

        return Result<TeamDto>.Success(new TeamDto
        {
            Id = team.Id,
            Name = team.Name,
            OwnerId = team.OwnerId,
            OwnerName = owner?.Name ?? "Unknown",
            MaxMembers = team.MaxMembers,
            CurrentMembers = members.Count() + 1, // +1 for owner
            CreatedAt = team.CreatedAt
        });
    }

    public async Task<Result<TeamDto>> CreateTeamAsync(string userId, string teamName)
    {
        // Check if user already owns a team
        var existingTeams = await _unitOfWork.Repository<Team>()
            .FindAsync(t => t.OwnerId == userId);
        if (existingTeams.Any())
            return Result<TeamDto>.Failure("You already have a team");

        // All active users have team feature — no tier-based gating
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
        if (user == null)
            return Result<TeamDto>.Failure("User not found");

        // Default max members for all users (admin can adjust per-team if needed)
        const int defaultMaxMembers = 100;

        var team = new Team
        {
            Name = string.IsNullOrWhiteSpace(teamName) ? $"{user.BusinessName} Team" : teamName,
            OwnerId = userId,
            MaxMembers = defaultMaxMembers
        };

        await _unitOfWork.Repository<Team>().AddAsync(team);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Team {TeamId} created by user {UserId} with max {MaxMembers} members",
            team.Id, userId, defaultMaxMembers);

        return Result<TeamDto>.Success(new TeamDto
        {
            Id = team.Id,
            Name = team.Name,
            OwnerId = team.OwnerId,
            OwnerName = user.Name,
            MaxMembers = team.MaxMembers,
            CurrentMembers = 1, // Just the owner
            CreatedAt = team.CreatedAt
        });
    }

    public async Task<Result<List<TeamMemberDto>>> GetMembersAsync(string userId)
    {
        var team = await GetUserTeamAsync(userId);
        if (team == null)
            return Result<List<TeamMemberDto>>.Failure("You are not part of any team");

        var members = await _unitOfWork.Repository<TeamMember>()
            .FindAsync(m => m.TeamId == team.Id);

        var memberDtos = new List<TeamMemberDto>();

        // Batch-load all users (owner + members) in a single query to avoid N+1
        var allUserIds = members.Select(m => m.UserId).Append(team.OwnerId).Distinct().ToList();
        var allUsers = await _unitOfWork.Repository<User>()
            .FindAsync(u => allUserIds.Contains(u.Id));
        var usersById = allUsers.ToDictionary(u => u.Id);

        // Add owner first
        if (usersById.TryGetValue(team.OwnerId, out var owner))
        {
            memberDtos.Add(new TeamMemberDto
            {
                Id = "owner",
                UserId = owner.Id,
                Name = owner.Name,
                Email = owner.Email,
                Role = TeamRole.Owner.ToString(),
                JoinedAt = team.CreatedAt
            });
        }

        foreach (var member in members)
        {
            if (usersById.TryGetValue(member.UserId, out var user))
            {
                memberDtos.Add(new TeamMemberDto
                {
                    Id = member.Id,
                    UserId = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Role = member.Role.ToString(),
                    JoinedAt = member.JoinedAt
                });
            }
        }

        return Result<List<TeamMemberDto>>.Success(memberDtos);
    }

    public async Task<Result<TeamInviteDto>> InviteMemberAsync(string userId, string email, TeamRole role)
    {
        var team = await GetOwnedOrAdminTeamAsync(userId);
        if (team == null)
            return Result<TeamInviteDto>.Failure("You must be a team owner or admin to invite members");

        // Check team member limit
        var members = await _unitOfWork.Repository<TeamMember>()
            .FindAsync(m => m.TeamId == team.Id);
        var pendingInvites = await _unitOfWork.Repository<TeamInvite>()
            .FindAsync(i => i.TeamId == team.Id && i.Status == InviteStatus.Pending);

        if (members.Count() + pendingInvites.Count() + 1 >= team.MaxMembers) // +1 for owner
            return Result<TeamInviteDto>.Failure($"Team member limit reached ({team.MaxMembers}). Upgrade your plan for more members.");

        // Check for existing membership
        var existingUser = (await _unitOfWork.Repository<User>()
            .FindAsync(u => u.Email == email)).FirstOrDefault();
        if (existingUser != null)
        {
            var existingMember = (await _unitOfWork.Repository<TeamMember>()
                .FindAsync(m => m.TeamId == team.Id && m.UserId == existingUser.Id)).FirstOrDefault();
            if (existingMember != null)
                return Result<TeamInviteDto>.Failure("This user is already a member of your team");

            if (existingUser.Id == team.OwnerId)
                return Result<TeamInviteDto>.Failure("Cannot invite the team owner");
        }

        // Check for existing pending invite
        var existingInvite = (await _unitOfWork.Repository<TeamInvite>()
            .FindAsync(i => i.TeamId == team.Id && i.Email == email && i.Status == InviteStatus.Pending)).FirstOrDefault();
        if (existingInvite != null)
            return Result<TeamInviteDto>.Failure("An invitation has already been sent to this email");

        // Owners and admins cannot be invited as members with lower role than inviter
        var inviterRole = await GetUserTeamRoleAsync(userId, team);
        if (role <= inviterRole && inviterRole != TeamRole.Owner)
            return Result<TeamInviteDto>.Failure("You cannot invite someone with a role equal to or higher than yours");

        var token = GenerateInviteToken();
        var tokenHash = HashToken(token);
        var invite = new TeamInvite
        {
            TeamId = team.Id,
            Email = email,
            Role = role,
            InvitedByUserId = userId,
            Token = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        await _unitOfWork.Repository<TeamInvite>().AddAsync(invite);
        await _unitOfWork.SaveChangesAsync();

        // Send invite email (outside transaction - non-critical)
        var inviter = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
        try
        {
            await _emailService.SendTeamInviteEmailAsync(email, team.Name, inviter?.Name ?? "A team member", token);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send team invite email to {Email}", email);
        }

        _logger.LogInformation("Team invite sent to {Email} for team {TeamId} by user {UserId}",
            email, team.Id, userId);

        return Result<TeamInviteDto>.Success(new TeamInviteDto
        {
            Id = invite.Id,
            TeamId = team.Id,
            TeamName = team.Name,
            Email = email,
            Role = role.ToString(),
            Status = InviteStatus.Pending.ToString(),
            InvitedBy = inviter?.Name ?? "Unknown",
            ExpiresAt = invite.ExpiresAt,
            CreatedAt = invite.CreatedAt
        });
    }

    public async Task<Result<bool>> AcceptInviteAsync(string token, string userId)
    {
        var tokenHash = HashToken(token);
        var invites = await _unitOfWork.Repository<TeamInvite>()
            .FindAsync(i => i.Token == tokenHash && i.Status == InviteStatus.Pending);
        var invite = invites.FirstOrDefault();

        if (invite == null)
            return Result<bool>.Failure("Invite not found or already responded to");

        if (invite.ExpiresAt < DateTime.UtcNow)
        {
            invite.Status = InviteStatus.Expired;
            _unitOfWork.Repository<TeamInvite>().Update(invite);
            await _unitOfWork.SaveChangesAsync();
            return Result<bool>.Failure("This invitation has expired");
        }

        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
        if (user == null)
            return Result<bool>.Failure("User not found");

        if (!string.Equals(user.Email, invite.Email, StringComparison.OrdinalIgnoreCase))
            return Result<bool>.Failure("This invitation was sent to a different email address");

        // Check if already a member
        var existingMember = (await _unitOfWork.Repository<TeamMember>()
            .FindAsync(m => m.TeamId == invite.TeamId && m.UserId == userId)).FirstOrDefault();
        if (existingMember != null)
            return Result<bool>.Failure("You are already a member of this team");

        // Re-check team member limit (may have changed since invite was sent)
        var team = (await _unitOfWork.Repository<Team>()
            .FindAsync(t => t.Id == invite.TeamId)).FirstOrDefault();
        if (team != null)
        {
            var currentMembers = await _unitOfWork.Repository<TeamMember>()
                .FindAsync(m => m.TeamId == team.Id);
            if (currentMembers.Count() + 1 >= team.MaxMembers) // +1 for owner
                return Result<bool>.Failure("This team has reached its maximum member limit. Ask the team owner to upgrade their plan.");
        }

        // Add member
        var member = new TeamMember
        {
            TeamId = invite.TeamId,
            UserId = userId,
            Role = invite.Role,
            JoinedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<TeamMember>().AddAsync(member);

        invite.Status = InviteStatus.Accepted;
        invite.AcceptedByUserId = userId;
        invite.RespondedAt = DateTime.UtcNow;
        _unitOfWork.Repository<TeamInvite>().Update(invite);

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User {UserId} accepted invite to team {TeamId}", userId, invite.TeamId);

        return Result<bool>.Success(true, "Successfully joined the team");
    }

    public async Task<Result<bool>> DeclineInviteAsync(string token, string userId)
    {
        var tokenHash = HashToken(token);
        var invites = await _unitOfWork.Repository<TeamInvite>()
            .FindAsync(i => i.Token == tokenHash && i.Status == InviteStatus.Pending);
        var invite = invites.FirstOrDefault();

        if (invite == null)
            return Result<bool>.Failure("Invite not found or already responded to");

        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
        if (user == null || !string.Equals(user.Email, invite.Email, StringComparison.OrdinalIgnoreCase))
            return Result<bool>.Failure("This invitation was sent to a different email address");

        invite.Status = InviteStatus.Declined;
        invite.RespondedAt = DateTime.UtcNow;
        _unitOfWork.Repository<TeamInvite>().Update(invite);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User {UserId} declined invite to team {TeamId}", userId, invite.TeamId);

        return Result<bool>.Success(true, "Invitation declined");
    }

    public async Task<Result<bool>> RevokeInviteAsync(string inviteId, string userId)
    {
        var team = await GetOwnedOrAdminTeamAsync(userId);
        if (team == null)
            return Result<bool>.Failure("You must be a team owner or admin to revoke invites");

        var invite = await _unitOfWork.Repository<TeamInvite>().GetByIdAsync(inviteId);
        if (invite == null || invite.TeamId != team.Id)
            return Result<bool>.Failure("Invite not found");

        if (invite.Status != InviteStatus.Pending)
            return Result<bool>.Failure("Only pending invites can be revoked");

        invite.Status = InviteStatus.Revoked;
        invite.RespondedAt = DateTime.UtcNow;
        _unitOfWork.Repository<TeamInvite>().Update(invite);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Invite {InviteId} revoked by user {UserId}", inviteId, userId);

        return Result<bool>.Success(true, "Invitation revoked");
    }

    public async Task<Result<bool>> RemoveMemberAsync(string memberId, string userId)
    {
        var team = await GetOwnedOrAdminTeamAsync(userId);
        if (team == null)
            return Result<bool>.Failure("You must be a team owner or admin to remove members");

        var member = await _unitOfWork.Repository<TeamMember>().GetByIdAsync(memberId);
        if (member == null || member.TeamId != team.Id)
            return Result<bool>.Failure("Member not found");

        // Prevent removing yourself
        if (member.UserId == userId)
            return Result<bool>.Failure("You cannot remove yourself from the team");

        // Check role hierarchy - can only remove members with lower role
        var removerRole = await GetUserTeamRoleAsync(userId, team);
        if (member.Role <= removerRole && removerRole != TeamRole.Owner)
            return Result<bool>.Failure("You cannot remove a member with a role equal to or higher than yours");

        member.IsDeleted = true;
        member.DeletedAt = DateTime.UtcNow;
        member.DeletedBy = userId;
        _unitOfWork.Repository<TeamMember>().Update(member);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Member {MemberId} removed from team {TeamId} by user {UserId}",
            memberId, team.Id, userId);

        return Result<bool>.Success(true, "Member removed from team");
    }

    public async Task<Result<bool>> UpdateMemberRoleAsync(string memberId, TeamRole newRole, string userId)
    {
        var team = await GetOwnedOrAdminTeamAsync(userId);
        if (team == null)
            return Result<bool>.Failure("You must be a team owner or admin to update roles");

        if (newRole == TeamRole.Owner)
            return Result<bool>.Failure("Cannot assign Owner role. Use transfer ownership instead.");

        var member = await _unitOfWork.Repository<TeamMember>().GetByIdAsync(memberId);
        if (member == null || member.TeamId != team.Id)
            return Result<bool>.Failure("Member not found");

        var updaterRole = await GetUserTeamRoleAsync(userId, team);
        if (updaterRole != TeamRole.Owner && newRole <= updaterRole)
            return Result<bool>.Failure("You cannot assign a role equal to or higher than yours");

        // Prevent non-owners from modifying members at or above their own role
        if (updaterRole != TeamRole.Owner && member.Role <= updaterRole)
            return Result<bool>.Failure("You cannot modify the role of a member with equal or higher rank");

        member.Role = newRole;
        member.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Repository<TeamMember>().Update(member);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Member {MemberId} role updated to {Role} by user {UserId}",
            memberId, newRole, userId);

        return Result<bool>.Success(true, "Member role updated");
    }

    public async Task<Result<List<TeamInviteDto>>> GetPendingInvitesAsync(string userId)
    {
        var team = await GetOwnedOrAdminTeamAsync(userId);
        if (team == null)
            return Result<List<TeamInviteDto>>.Failure("You must be a team owner or admin to view invites");

        var invites = await _unitOfWork.Repository<TeamInvite>()
            .FindAsync(i => i.TeamId == team.Id && i.Status == InviteStatus.Pending);

        // Batch-load all inviters in a single query to avoid N+1
        var inviterIds = invites.Select(i => i.InvitedByUserId).Distinct().ToList();
        var inviters = await _unitOfWork.Repository<User>()
            .FindAsync(u => inviterIds.Contains(u.Id));
        var invitersById = inviters.ToDictionary(u => u.Id);

        var inviteDtos = invites.Select(invite => new TeamInviteDto
        {
            Id = invite.Id,
            TeamId = team.Id,
            TeamName = team.Name,
            Email = invite.Email,
            Role = invite.Role.ToString(),
            Status = invite.Status.ToString(),
            InvitedBy = invitersById.TryGetValue(invite.InvitedByUserId, out var inviter) ? inviter.Name : "Unknown",
            ExpiresAt = invite.ExpiresAt,
            CreatedAt = invite.CreatedAt
        }).ToList();

        return Result<List<TeamInviteDto>>.Success(inviteDtos);
    }

    public async Task<Result<TeamInviteDto>> GetInviteByTokenAsync(string token)
    {
        // Only return pending invites to prevent information leakage for expired/revoked/accepted invites
        var tokenHash = HashToken(token);
        var invites = await _unitOfWork.Repository<TeamInvite>()
            .FindAsync(i => i.Token == tokenHash && i.Status == InviteStatus.Pending);
        var invite = invites.FirstOrDefault();

        if (invite == null)
            return Result<TeamInviteDto>.Failure("Invite not found or is no longer valid");

        var teams = await _unitOfWork.Repository<Team>()
            .FindAsync(t => t.Id == invite.TeamId);
        var team = teams.FirstOrDefault();

        var inviter = await _unitOfWork.Repository<User>().GetByIdAsync(invite.InvitedByUserId);

        return Result<TeamInviteDto>.Success(new TeamInviteDto
        {
            Id = invite.Id,
            TeamId = invite.TeamId,
            TeamName = team?.Name ?? "Unknown",
            Email = invite.Email,
            Role = invite.Role.ToString(),
            Status = invite.Status.ToString(),
            InvitedBy = inviter?.Name ?? "Unknown",
            ExpiresAt = invite.ExpiresAt,
            CreatedAt = invite.CreatedAt
        });
    }

    private async Task<Team?> GetUserTeamAsync(string userId)
    {
        // Check if user owns a team
        var ownedTeams = await _unitOfWork.Repository<Team>()
            .FindAsync(t => t.OwnerId == userId);
        var team = ownedTeams.FirstOrDefault();
        if (team != null) return team;

        // Check if user is a member
        var memberships = await _unitOfWork.Repository<TeamMember>()
            .FindAsync(m => m.UserId == userId);
        var membership = memberships.FirstOrDefault();
        if (membership == null) return null;

        var memberTeams = await _unitOfWork.Repository<Team>()
            .FindAsync(t => t.Id == membership.TeamId);
        return memberTeams.FirstOrDefault();
    }

    private async Task<Team?> GetOwnedOrAdminTeamAsync(string userId)
    {
        // Check if user owns a team
        var ownedTeams = await _unitOfWork.Repository<Team>()
            .FindAsync(t => t.OwnerId == userId);
        var team = ownedTeams.FirstOrDefault();
        if (team != null) return team;

        // Check if user is an admin in a team
        var memberships = await _unitOfWork.Repository<TeamMember>()
            .FindAsync(m => m.UserId == userId && (m.Role == TeamRole.Admin || m.Role == TeamRole.Manager));
        var membership = memberships.FirstOrDefault();
        if (membership == null) return null;

        var memberTeams = await _unitOfWork.Repository<Team>()
            .FindAsync(t => t.Id == membership.TeamId);
        return memberTeams.FirstOrDefault();
    }

    private async Task<TeamRole> GetUserTeamRoleAsync(string userId, Team team)
    {
        if (team.OwnerId == userId) return TeamRole.Owner;

        var memberships = await _unitOfWork.Repository<TeamMember>()
            .FindAsync(m => m.TeamId == team.Id && m.UserId == userId);
        var membership = memberships.FirstOrDefault();
        return membership?.Role ?? TeamRole.Member;
    }

    private static string GenerateInviteToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(
            SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
    }
}

#pragma warning restore CS0618
