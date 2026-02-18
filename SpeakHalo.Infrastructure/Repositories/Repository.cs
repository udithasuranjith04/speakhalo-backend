using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;
using SpeakHalo.Domain.Models;
using SpeakHalo.Infrastructure.Data;

namespace SpeakHalo.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().ToListAsync(cancellationToken);
    }

    public virtual async Task<PaginatedList<T>> GetPagedAsync(int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var totalCount = await _dbSet.CountAsync(cancellationToken);

        var items = await _dbSet
            .AsNoTracking()
            .OrderByDescending(e => e.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedList<T>(items, totalCount, pageNumber, pageSize);
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);
    }

    public virtual async Task<PaginatedList<T>> FindPagedAsync(Expression<Func<T, bool>> predicate, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().Where(predicate);
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedList<T>(items, totalCount, pageNumber, pageSize);
    }

    public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(predicate, cancellationToken);
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        return predicate == null
            ? await _dbSet.CountAsync(cancellationToken)
            : await _dbSet.CountAsync(predicate, cancellationToken);
    }

    public virtual async Task<TResult?> SumAsync<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken = default)
    {
        // Use reflection to call the appropriate SumAsync method based on the result type
        var query = _dbSet.AsNoTracking().Select(selector);
        var resultType = typeof(TResult);

        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(resultType) ?? resultType;

        if (underlyingType == typeof(int))
        {
            var sum = await ((IQueryable<int?>)query).SumAsync(cancellationToken);
            return (TResult?)(object?)sum;
        }
        else if (underlyingType == typeof(long))
        {
            var sum = await ((IQueryable<long?>)query).SumAsync(cancellationToken);
            return (TResult?)(object?)sum;
        }
        else if (underlyingType == typeof(decimal))
        {
            var sum = await ((IQueryable<decimal?>)query).SumAsync(cancellationToken);
            return (TResult?)(object?)sum;
        }
        else if (underlyingType == typeof(double))
        {
            var sum = await ((IQueryable<double?>)query).SumAsync(cancellationToken);
            return (TResult?)(object?)sum;
        }
        else if (underlyingType == typeof(float))
        {
            var sum = await ((IQueryable<float?>)query).SumAsync(cancellationToken);
            return (TResult?)(object?)sum;
        }

        throw new NotSupportedException($"SumAsync does not support type {resultType.Name}");
    }

    public virtual async Task<double?> AverageAsync<TProperty>(Expression<Func<T, TProperty>> selector, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().Select(selector);
        var propertyType = typeof(TProperty);
        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (underlyingType == typeof(int))
        {
            return await ((IQueryable<int?>)query).AverageAsync(cancellationToken);
        }
        else if (underlyingType == typeof(long))
        {
            return await ((IQueryable<long?>)query).AverageAsync(cancellationToken);
        }
        else if (underlyingType == typeof(decimal))
        {
            return (double?)(await ((IQueryable<decimal?>)query).AverageAsync(cancellationToken));
        }
        else if (underlyingType == typeof(double))
        {
            return await ((IQueryable<double?>)query).AverageAsync(cancellationToken);
        }
        else if (underlyingType == typeof(float))
        {
            return await ((IQueryable<float?>)query).AverageAsync(cancellationToken);
        }

        throw new NotSupportedException($"AverageAsync does not support type {propertyType.Name}");
    }

    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);
    }

    public virtual void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public virtual void UpdateRange(IEnumerable<T> entities)
    {
        _dbSet.UpdateRange(entities);
    }

    public virtual void Remove(T entity)
    {
        // Soft delete: mark as deleted instead of removing
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        _dbSet.Update(entity);
    }

    public virtual void RemoveRange(IEnumerable<T> entities)
    {
        // Soft delete: mark all as deleted instead of removing
        foreach (var entity in entities)
        {
            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
        }
        _dbSet.UpdateRange(entities);
    }

    public virtual void HardDelete(T entity)
    {
        // Permanent delete: actually remove from database
        _dbSet.Remove(entity);
    }

    public virtual void HardDeleteRange(IEnumerable<T> entities)
    {
        // Permanent delete: actually remove from database
        _dbSet.RemoveRange(entities);
    }
}
