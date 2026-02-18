using SpeakHalo.Domain.Entities;

namespace SpeakHalo.Domain.Interfaces.Repositories;

public interface ICallRepository : IRepository<Call>
{
    Task<IEnumerable<Call>> GetUserCallsAsync(string userId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task<Call?> GetWithTranscriptAsync(string callId, CancellationToken cancellationToken = default);
}
