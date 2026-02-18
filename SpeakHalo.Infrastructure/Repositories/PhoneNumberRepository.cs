using Microsoft.EntityFrameworkCore;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;
using SpeakHalo.Infrastructure.Data;

namespace SpeakHalo.Infrastructure.Repositories;

public class PhoneNumberRepository : Repository<PhoneNumber>, IPhoneNumberRepository
{
    public PhoneNumberRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<PhoneNumber>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<PhoneNumber>()
            .Where(p => p.UserId == userId && !p.IsDeleted)
            .OrderBy(p => p.IsPrimary ? 0 : 1)
            .ThenByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<PhoneNumber?> GetPrimaryNumberAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<PhoneNumber>()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.IsPrimary && !p.IsDeleted, cancellationToken);
    }

    public async Task<PhoneNumber?> GetByNumberAsync(string number, CancellationToken cancellationToken = default)
    {
        return await _context.Set<PhoneNumber>()
            .FirstOrDefaultAsync(p => p.Number == number && !p.IsDeleted, cancellationToken);
    }
}
