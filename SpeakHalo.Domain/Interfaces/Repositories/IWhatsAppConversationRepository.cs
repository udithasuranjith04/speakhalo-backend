using SpeakHalo.Domain.Entities;

namespace SpeakHalo.Domain.Interfaces.Repositories;

public interface IWhatsAppConversationRepository : IRepository<WhatsAppConversation>
{
    Task<WhatsAppConversation?> GetByPhoneNumbersAsync(string businessPhone, string customerPhone, CancellationToken cancellationToken = default);
    Task<IEnumerable<WhatsAppConversation>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<WhatsAppConversation?> GetWithMessagesAsync(string conversationId, int limit = 50, CancellationToken cancellationToken = default);
    Task<IEnumerable<WhatsAppConversation>> GetActiveConversationsAsync(string userId, CancellationToken cancellationToken = default);
}
