using SpeakHalo.Domain.Entities;

namespace SpeakHalo.Domain.Interfaces.Repositories;

public interface ISMSMessageRepository : IRepository<SMSMessage>
{
    Task<IEnumerable<SMSMessage>> GetByConversationIdAsync(string conversationId, CancellationToken cancellationToken = default);
    Task<SMSMessage?> GetByTwilioSidAsync(string twilioSid, CancellationToken cancellationToken = default);
}
