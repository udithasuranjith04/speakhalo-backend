using System.Linq.Expressions;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Models;

namespace SpeakHalo.Domain.Interfaces.Repositories;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PaginatedList<T>> GetPagedAsync(int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<PaginatedList<T>> FindPagedAsync(Expression<Func<T, bool>> predicate, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);
    Task<TResult?> SumAsync<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken = default);
    Task<double?> AverageAsync<TProperty>(Expression<Func<T, TProperty>> selector, CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    void Update(T entity);
    void UpdateRange(IEnumerable<T> entities);
    void Remove(T entity); // Soft delete
    void RemoveRange(IEnumerable<T> entities); // Soft delete
    void HardDelete(T entity); // Permanent delete
    void HardDeleteRange(IEnumerable<T> entities); // Permanent delete
}
