using Microsoft.EntityFrameworkCore;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;
using SpeakHalo.Infrastructure.Data;

namespace SpeakHalo.Infrastructure.Repositories;

public class SMSConversationRepository : Repository<SMSConversation>, ISMSConversationRepository
{
    public SMSConversationRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<SMSConversation?> GetByPhoneNumbersAsync(string businessPhone, string customerPhone, CancellationToken cancellationToken = default)
    {
        return await _context.SMSConversations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.BusinessPhone == businessPhone && c.CustomerPhone == customerPhone && c.IsActive, cancellationToken);
    }

    public async Task<IEnumerable<SMSConversation>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.SMSConversations
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<SMSConversation?> GetWithMessagesAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        return await _context.SMSConversations
            .AsNoTracking()
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);
    }
}
