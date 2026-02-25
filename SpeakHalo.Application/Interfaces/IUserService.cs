using SpeakHalo.Application.DTOs;
using SpeakHalo.Shared.Helpers;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Interfaces;

public interface IUserService
{
    Task<Result<UserResponse>> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<UserResponse>> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Result<UserResponse>> UpdateUserAsync(string userId, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool>> UpdatePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeactivateUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<PaginatedList<UserResponse>>> GetAllUsersAsync(int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<Result<bool>> UpdateOnboardingAsync(string userId, UpdateOnboardingRequest request, CancellationToken cancellationToken = default);
}
