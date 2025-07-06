using System.Linq.Expressions;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BudgetManager.Infrastructure.Persistence.Services;

public class BudgetManagerService(ApplicationDbContext dbContext) : IBudgetManagerService
{
  public async Task<IEnumerable<Fund>> GetAllFundsWithTransactionsAsync(Guid budgetId, CancellationToken cancellationToken)
  {
    return await dbContext.Funds
      .Include(f => f.Allocations)
      .Include(f => f.Deallocations)
      .Include(f => f.Reallocations)
      .Where(x => x.BudgetId == budgetId)
      .ToArrayAsync(cancellationToken);
  }

  public async Task<T> AddAsync<T>(T entity, CancellationToken cancellationToken) where T : Entity
  {
    if (entity is null)
    {
      throw new ArgumentNullException(nameof(entity), "Entity cannot be null");
    }

    dbContext.Set<T>().Add(entity);

    await dbContext.SaveChangesAsync(cancellationToken);

    return entity;
  }

  public async Task<T> GetAsync<T>(Guid id, CancellationToken cancellationToken) where T : Entity
  {
    if (id == Guid.Empty)
    {
      throw new ArgumentException("ID cannot be empty", nameof(id));
    }

    return await dbContext.Set<T>().FindAsync([id], cancellationToken)
      ?? throw new KeyNotFoundException($"Entity of type {typeof(T).Name} with ID {id} not found.");
  }

  public async Task<IEnumerable<T>> GetAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken) where T : Entity
  {
    ArgumentNullException.ThrowIfNull(predicate);

    return await dbContext.Set<T>()
        .Where(predicate)
        .ToListAsync(cancellationToken);
  }

  public async Task<bool> ExistsAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken) where T : Entity
  {
    ArgumentNullException.ThrowIfNull(predicate);

    return await dbContext.Set<T>().AnyAsync(predicate,cancellationToken);
  }
}