using Microsoft.EntityFrameworkCore;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;
using SpeakHalo.Infrastructure.Data;

namespace SpeakHalo.Infrastructure.Repositories;

public class WhatsAppMessageRepository : Repository<WhatsAppMessage>, IWhatsAppMessageRepository
{
    public WhatsAppMessageRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<WhatsAppMessage>> GetByConversationIdAsync(string conversationId, int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _context.WhatsAppMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<WhatsAppMessage?> GetByTwilioSidAsync(string twilioSid, CancellationToken cancellationToken = default)
    {
        return await _context.WhatsAppMessages
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.TwilioMessageSid == twilioSid, cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        return await _context.WhatsAppMessages
            .Where(m => m.ConversationId == conversationId && m.ReadAt == null)
            .CountAsync(cancellationToken);
    }
}
