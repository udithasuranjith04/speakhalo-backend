using Microsoft.EntityFrameworkCore;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;
using SpeakHalo.Infrastructure.Data;

namespace SpeakHalo.Infrastructure.Repositories;

public class SMSMessageRepository : Repository<SMSMessage>, ISMSMessageRepository
{
    public SMSMessageRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<SMSMessage>> GetByConversationIdAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        return await _context.SMSMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<SMSMessage?> GetByTwilioSidAsync(string twilioSid, CancellationToken cancellationToken = default)
    {
        return await _context.SMSMessages
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.TwilioMessageSid == twilioSid, cancellationToken);
    }
}
