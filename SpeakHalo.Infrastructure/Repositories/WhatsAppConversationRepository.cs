using Microsoft.EntityFrameworkCore;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;
using SpeakHalo.Infrastructure.Data;

namespace SpeakHalo.Infrastructure.Repositories;

public class WhatsAppConversationRepository : Repository<WhatsAppConversation>, IWhatsAppConversationRepository
{
    public WhatsAppConversationRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<WhatsAppConversation?> GetByPhoneNumbersAsync(string businessPhone, string customerPhone, CancellationToken cancellationToken = default)
    {
        return await _context.WhatsAppConversations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.BusinessPhone == businessPhone && c.CustomerPhone == customerPhone, cancellationToken);
    }

    public async Task<IEnumerable<WhatsAppConversation>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.WhatsAppConversations
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<WhatsAppConversation?> GetWithMessagesAsync(string conversationId, int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _context.WhatsAppConversations
            .AsNoTracking()
            .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(limit))
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);
    }

    public async Task<IEnumerable<WhatsAppConversation>> GetActiveConversationsAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.WhatsAppConversations
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.IsActive)
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
