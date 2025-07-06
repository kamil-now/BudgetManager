using System.Linq.Expressions;
using BudgetManager.Domain.Entities;

namespace BudgetManager.Domain.Interfaces;

public interface IBudgetManagerService
{
  Task<IEnumerable<Fund>> GetAllFundsWithTransactionsAsync(Guid budgetId, CancellationToken cancellationToken = default);

  Task<T> GetAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : Entity;
  Task<IEnumerable<T>> GetAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) where T : Entity;
  Task<T> AddAsync<T>(T entity, CancellationToken cancellationToken = default) where T : Entity;
  Task<bool> ExistsAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) where T : Entity;
}