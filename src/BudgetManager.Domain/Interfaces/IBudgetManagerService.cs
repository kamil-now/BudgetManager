using System.Linq.Expressions;
using BudgetManager.Domain.Entities;

namespace BudgetManager.Domain.Interfaces;

public interface IBudgetManagerService
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task RunInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default);

    Task<IEnumerable<Fund>> GetAllFundsWithTransactionsAsync(Guid budgetId, CancellationToken cancellationToken = default);

    Task<Ledger> GetLedgerAsync(Guid id, CancellationToken cancellationToken = default);

    Task<T> GetAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : Entity;
    Task<IEnumerable<T>> GetAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) where T : Entity;
    Task<T> CreateAsync<T>(T entity, CancellationToken cancellationToken = default) where T : Entity;
    Task<bool> ExistsAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) where T : Entity;
    Task UpdateAsync<T>(Guid id, IEnumerable<Expression<Func<T, object>>> updatedProperties, CancellationToken cancellationToken) where T : Entity;
    Task DeleteAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : Entity;
}
