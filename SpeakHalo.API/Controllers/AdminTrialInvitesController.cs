using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeakHalo.Application.DTOs.Payment;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;
using System.Security.Claims;

namespace SpeakHalo.API.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/v1/admin/trial-invites")]
public class AdminTrialInvitesController : ControllerBase
{
    private readonly ITrialInviteService _trialInviteService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdminTrialInvitesController> _logger;
    private readonly string _frontendBaseUrl;

    public AdminTrialInvitesController(
        ITrialInviteService trialInviteService,
        IUnitOfWork unitOfWork,
        ILogger<AdminTrialInvitesController> logger,
        IConfiguration configuration)
    {
        _trialInviteService = trialInviteService;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _frontendBaseUrl = configuration["Frontend:BaseUrl"]?.TrimEnd('/') ?? "https://app.speakhalo.com";
    }

    [HttpGet]
    public async Task<IActionResult> ListInvites(CancellationToken cancellationToken)
    {
        var result = await _trialInviteService.ListInvitesAsync(_frontendBaseUrl, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> CreateInvite(
        [FromBody] CreateTrialInviteRequest request,
        CancellationToken cancellationToken)
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminId))
            return Unauthorized();

        var result = await _trialInviteService.CreateInviteAsync(request, adminId, _frontendBaseUrl, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result.Data);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var cutoff30 = now.AddDays(-30);

        var all = await _unitOfWork.Repository<TrialInvite>()
            .FindAsync(i => !i.IsDeleted, cancellationToken);

        var total = all.Count();
        var redeemed = all.Count(i => i.IsRedeemed);
        var expired = all.Count(i => !i.IsRedeemed && i.ExpiresAt < now);
        var pending = all.Count(i => !i.IsRedeemed && i.ExpiresAt >= now);

        var resolved = redeemed + expired;
        var conversionRate = resolved > 0 ? Math.Round((double)redeemed / resolved * 100, 1) : 0.0;

        var last30DaysCreated = all.Count(i => i.CreatedAt >= cutoff30);
        var last30DaysRedeemed = all.Count(i => i.IsRedeemed && i.RedeemedAt.HasValue && i.RedeemedAt >= cutoff30);

        return Ok(new
        {
            total,
            pending,
            redeemed,
            expired,
            conversionRate,
            last30Days = new
            {
                created = last30DaysCreated,
                redeemed = last30DaysRedeemed,
            }
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> RevokeInvite(string id, CancellationToken cancellationToken)
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminId))
            return Unauthorized();

        var result = await _trialInviteService.RevokeInviteAsync(id, adminId, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Errors.FirstOrDefault() });

        return Ok(new { message = "Invite revoked successfully" });
    }
}
