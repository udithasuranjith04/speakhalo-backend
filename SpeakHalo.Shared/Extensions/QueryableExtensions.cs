using Microsoft.EntityFrameworkCore;
using SpeakHalo.Shared.Constants;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Shared.Extensions;

public static class QueryableExtensions
{
    public static async Task<PaginatedList<T>> ToPaginatedListAsync<T>(
        this IQueryable<T> query,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = AppConstants.DefaultPageSize;
        if (pageSize > AppConstants.MaxPageSize) pageSize = AppConstants.MaxPageSize;

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedList<T>(items, totalCount, pageNumber, pageSize);
    }
}
