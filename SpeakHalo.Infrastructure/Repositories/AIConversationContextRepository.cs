using Microsoft.EntityFrameworkCore;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;
using SpeakHalo.Infrastructure.Data;

namespace SpeakHalo.Infrastructure.Repositories;

public class AIConversationContextRepository : Repository<AIConversationContext>, IAIConversationContextRepository
{
    public AIConversationContextRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<AIConversationContext?> GetByConversationIdAsync(
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        return await _context.AIConversationContexts
            .FirstOrDefaultAsync(c => c.ConversationId == conversationId, cancellationToken);
    }

    public async Task<List<AIConversationContext>> GetActiveContextsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.AIConversationContexts
            .Where(c => c.UserId == userId && c.IsActive && c.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(c => c.LastInteractionAt)
            .ToListAsync(cancellationToken);
    }

    public async Task CleanupExpiredContextsAsync(
        CancellationToken cancellationToken = default)
    {
        var expiredContexts = await _context.AIConversationContexts
            .Where(c => c.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var context in expiredContexts)
        {
            context.IsActive = false;
        }

        if (expiredContexts.Any())
        {
            _context.AIConversationContexts.UpdateRange(expiredContexts);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
