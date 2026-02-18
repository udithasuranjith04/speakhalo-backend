using SpeakHalo.Domain.Entities;

namespace SpeakHalo.Domain.Interfaces.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetWithPhoneNumbersAsync(string userId, CancellationToken cancellationToken = default);
}
