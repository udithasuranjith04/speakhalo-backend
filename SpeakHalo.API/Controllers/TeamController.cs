using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeakHalo.API.Filters;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Enums;
using System.Security.Claims;

namespace SpeakHalo.API.Controllers;

[ApiController]
[Route("api/v1/team")]
[Authorize]
[RequireFeature(PlanFeature.TeamMembers)]
public class TeamController : ControllerBase
{
    private readonly ITeamService _teamService;

    public TeamController(ITeamService teamService)
    {
        _teamService = teamService;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> GetTeam()
    {
        var result = await _teamService.GetTeamAsync(GetUserId());
        if (!result.IsSuccess)
            return NotFound(new { error = result.Errors.FirstOrDefault() });
        return Ok(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTeam([FromBody] CreateTeamRequest request)
    {
        var result = await _teamService.CreateTeamAsync(GetUserId(), request.Name);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        return Created($"/api/v1/team/{result.Data!.Id}", result.Data);
    }

    [HttpGet("members")]
    public async Task<IActionResult> GetMembers()
    {
        var result = await _teamService.GetMembersAsync(GetUserId());
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        return Ok(result.Data);
    }

    [HttpPost("invites")]
    public async Task<IActionResult> InviteMember([FromBody] InviteMemberRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { error = "Email is required" });

        if (!Enum.TryParse<TeamRole>(request.Role, true, out var role) || role == TeamRole.Owner)
            return BadRequest(new { error = "Invalid role. Use Admin, Manager, or Member." });

        var result = await _teamService.InviteMemberAsync(GetUserId(), request.Email, role);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        return Created($"/api/v1/team/invites/{result.Data!.Id}", result.Data);
    }

    [HttpGet("invites")]
    public async Task<IActionResult> GetPendingInvites()
    {
        var result = await _teamService.GetPendingInvitesAsync(GetUserId());
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        return Ok(result.Data);
    }

    [HttpGet("invites/{token}/details")]
    [AllowAnonymous]
    public async Task<IActionResult> GetInviteDetails(string token)
    {
        var result = await _teamService.GetInviteByTokenAsync(token);
        if (!result.IsSuccess)
            return NotFound(new { error = result.Errors.FirstOrDefault() });
        return Ok(result.Data);
    }

    [HttpPost("invites/{token}/accept")]
    public async Task<IActionResult> AcceptInvite(string token)
    {
        var result = await _teamService.AcceptInviteAsync(token, GetUserId());
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        return Ok(new { message = result.Message });
    }

    [HttpPost("invites/{token}/decline")]
    public async Task<IActionResult> DeclineInvite(string token)
    {
        var result = await _teamService.DeclineInviteAsync(token, GetUserId());
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        return Ok(new { message = result.Message });
    }

    [HttpDelete("invites/{inviteId}")]
    public async Task<IActionResult> RevokeInvite(string inviteId)
    {
        var result = await _teamService.RevokeInviteAsync(inviteId, GetUserId());
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        return Ok(new { message = result.Message });
    }

    [HttpDelete("members/{memberId}")]
    public async Task<IActionResult> RemoveMember(string memberId)
    {
        var result = await _teamService.RemoveMemberAsync(memberId, GetUserId());
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        return Ok(new { message = result.Message });
    }

    [HttpPut("members/{memberId}/role")]
    public async Task<IActionResult> UpdateMemberRole(string memberId, [FromBody] UpdateRoleRequest request)
    {
        if (!Enum.TryParse<TeamRole>(request.Role, true, out var role) || role == TeamRole.Owner)
            return BadRequest(new { error = "Invalid role. Use Admin, Manager, or Member." });

        var result = await _teamService.UpdateMemberRoleAsync(memberId, role, GetUserId());
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        return Ok(new { message = result.Message });
    }
}

public class CreateTeamRequest
{
    public string Name { get; set; } = string.Empty;
}

public class InviteMemberRequest
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Member";
}

public class UpdateRoleRequest
{
    public string Role { get; set; } = string.Empty;
}
