using SpeakHalo.Domain.Entities;

namespace SpeakHalo.Domain.Interfaces.Repositories;

public interface IPhoneNumberRepository : IRepository<PhoneNumber>
{
    Task<List<PhoneNumber>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<PhoneNumber?> GetPrimaryNumberAsync(string userId, CancellationToken cancellationToken = default);
    Task<PhoneNumber?> GetByNumberAsync(string number, CancellationToken cancellationToken = default);
}
