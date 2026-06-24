namespace Backend.Domain.Interfaces;

/// <summary>
/// Generic repository interface defining basic CRUD operations.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
/// <typeparam name="TId">The identifier type.</typeparam>
public interface IRepository<T, TId>
{
    Task<T?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default);
    Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(TId id, CancellationToken cancellationToken = default);
}
