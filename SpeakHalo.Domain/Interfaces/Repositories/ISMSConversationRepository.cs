using SpeakHalo.Domain.Entities;

namespace SpeakHalo.Domain.Interfaces.Repositories;

public interface ISMSConversationRepository : IRepository<SMSConversation>
{
    Task<SMSConversation?> GetByPhoneNumbersAsync(string businessPhone, string customerPhone, CancellationToken cancellationToken = default);
    Task<IEnumerable<SMSConversation>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<SMSConversation?> GetWithMessagesAsync(string conversationId, CancellationToken cancellationToken = default);
}
