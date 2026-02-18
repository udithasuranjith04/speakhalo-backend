using SpeakHalo.Domain.Entities;

namespace SpeakHalo.Domain.Interfaces.Repositories;

public interface IWhatsAppMessageRepository : IRepository<WhatsAppMessage>
{
    Task<IEnumerable<WhatsAppMessage>> GetByConversationIdAsync(string conversationId, int limit = 50, CancellationToken cancellationToken = default);
    Task<WhatsAppMessage?> GetByTwilioSidAsync(string twilioSid, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(string conversationId, CancellationToken cancellationToken = default);
}
