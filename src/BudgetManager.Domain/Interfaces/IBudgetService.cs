using System.Linq.Expressions;
using BudgetManager.Domain.Entities;

namespace BudgetManager.Domain.Interfaces;

public interface IBudgetService
{
  Task<IEnumerable<Fund>> GetAllFundsWithTransactions(Guid budgetId, CancellationToken cancellationToken);

  Task<T> Get<T>(Guid id, CancellationToken cancellationToken) where T : Entity;
  Task<IEnumerable<T>> Get<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken) where T : Entity;
  Task<T> Add<T>(T entity, CancellationToken cancellationToken) where T : Entity;
  Task<bool> Exists<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken) where T : Entity;
}