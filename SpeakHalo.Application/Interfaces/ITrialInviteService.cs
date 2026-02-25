using SpeakHalo.Application.DTOs.Payment;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Interfaces;

public interface ITrialInviteService
{
    Task<Result<TrialInviteCreatedResponse>> CreateInviteAsync(CreateTrialInviteRequest request, string adminId, string frontendBaseUrl, CancellationToken cancellationToken = default);
    Task<Result<TrialInviteValidationResponse>> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<Result> RedeemTrialAsync(string token, string userId, CancellationToken cancellationToken = default);
    Task<Result<List<TrialInviteResponse>>> ListInvitesAsync(string frontendBaseUrl, CancellationToken cancellationToken = default);
    Task<Result> RevokeInviteAsync(string inviteId, string adminId, CancellationToken cancellationToken = default);
}
