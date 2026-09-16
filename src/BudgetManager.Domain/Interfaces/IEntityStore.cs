using System.Linq.Expressions;
using BudgetManager.Domain.Entities;

namespace BudgetManager.Domain.Interfaces;

public interface IEntityStore
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<T> RunInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default);

    Task<T> GetAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : Entity;
    Task<IEnumerable<T>> GetAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) where T : Entity;
    Task<T> CreateAsync<T>(T entity, CancellationToken cancellationToken = default) where T : Entity;
    Task<bool> ExistsAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) where T : Entity;
    Task DeleteAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : Entity;
}
