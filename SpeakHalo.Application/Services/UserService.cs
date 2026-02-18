using Microsoft.Extensions.Logging;
using SpeakHalo.Application.DTOs;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Interfaces.Repositories;
using SpeakHalo.Shared.Constants;
using SpeakHalo.Shared.Extensions;
using SpeakHalo.Shared.Helpers;
using SpeakHalo.Shared.Models;

#pragma warning disable CS0618

namespace SpeakHalo.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserService> _logger;

    public UserService(IUnitOfWork unitOfWork, ILogger<UserService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<UserResponse>> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);

            if (user == null)
                return Result<UserResponse>.Failure(ErrorMessages.NotFound("User"));

            return Result<UserResponse>.Success(MapToResponse(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user {UserId}", userId);
            return Result<UserResponse>.Failure($"Failed to get user: {ex.Message}");
        }
    }

    public async Task<Result<UserResponse>> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken);

            if (user == null)
                return Result<UserResponse>.Failure(ErrorMessages.NotFound("User"));

            return Result<UserResponse>.Success(MapToResponse(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user by email {Email}", email);
            return Result<UserResponse>.Failure($"Failed to get user: {ex.Message}");
        }
    }

    public async Task<Result<UserResponse>> UpdateUserAsync(string userId, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);

            if (user == null)
                return Result<UserResponse>.Failure(ErrorMessages.NotFound("User"));

            if (request.Name != null) user.Name = request.Name;
            if (request.BusinessName != null) user.BusinessName = request.BusinessName;

            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<UserResponse>.Success(MapToResponse(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user {UserId}", userId);
            return Result<UserResponse>.Failure($"Failed to update user: {ex.Message}");
        }
    }

    public async Task<Result<bool>> UpdatePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);

            if (user == null)
                return Result<bool>.Failure(ErrorMessages.NotFound("User"));

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.HashedPassword))
                return Result<bool>.Failure("Current password is incorrect");

            user.HashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);

            // Security: Invalidate all refresh tokens when password changes
            // This forces re-authentication on all devices
            var refreshTokens = await _unitOfWork.Repository<Domain.Entities.RefreshToken>()
                .FindAsync(rt => rt.UserId == userId && !rt.Revoked, cancellationToken);

            foreach (var token in refreshTokens)
            {
                token.Revoked = true;
                _unitOfWork.Repository<Domain.Entities.RefreshToken>().Update(token);
            }

            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update password for user {UserId}", userId);
            return Result<bool>.Failure($"Failed to update password: {ex.Message}");
        }
    }

    public async Task<Result<bool>> DeactivateUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);

            if (user == null)
                return Result<bool>.Failure(ErrorMessages.NotFound("User"));

            user.IsActive = false;

            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate user {UserId}", userId);
            return Result<bool>.Failure($"Failed to deactivate user: {ex.Message}");
        }
    }

    public async Task<Result<PaginatedList<UserResponse>>> GetAllUsersAsync(int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var users = await _unitOfWork.Users.GetAllAsync(cancellationToken);
            var orderedUsers = users.OrderByDescending(u => u.CreatedAt);

            var paginatedUsers = await orderedUsers
                .AsQueryable()
                .ToPaginatedListAsync(pageNumber, pageSize, cancellationToken);

            var responses = paginatedUsers.Items.Select(MapToResponse).ToList();

            var result = new PaginatedList<UserResponse>(
                responses,
                paginatedUsers.TotalCount,
                paginatedUsers.CurrentPage,
                paginatedUsers.PageSize
            );

            return Result<PaginatedList<UserResponse>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all users");
            return Result<PaginatedList<UserResponse>>.Failure($"Failed to get users: {ex.Message}");
        }
    }

    private static UserResponse MapToResponse(Domain.Entities.User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            BusinessName = user.BusinessName,
            Role = user.Role,
            Industry = user.Industry.ToString(),
            Phone = null,
            Website = null,
            IsActive = user.IsActive,
            EmailVerified = false,
            CreatedAt = user.CreatedAt
        };
    }
}

#pragma warning restore CS0618
