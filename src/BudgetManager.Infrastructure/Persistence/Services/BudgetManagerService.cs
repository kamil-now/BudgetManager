using System.Linq.Expressions;
using System.Threading.Tasks;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BudgetManager.Infrastructure.Persistence.Services;

public class BudgetManagerService(ApplicationDbContext dbContext) : IBudgetManagerService
{
  public async Task SaveChangesAsync(CancellationToken cancellationToken = default) => await dbContext.SaveChangesAsync(cancellationToken);

  public async Task RunInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default)
  {
    var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

    try
    {
      await action();
      await transaction.CommitAsync(cancellationToken);
    }
    catch
    {
      await transaction.RollbackAsync(cancellationToken);
      throw;
    }
  }

  public async Task<IEnumerable<Fund>> GetAllFundsWithTransactionsAsync(Guid budgetId, CancellationToken cancellationToken)
  {
    return await dbContext.Funds
      .Include(f => f.Allocations)
      .Include(f => f.Deallocations)
      .Include(f => f.Reallocations)
      .Where(x => x.BudgetId == budgetId)
      .ToArrayAsync(cancellationToken);
  }

  public async Task<T> CreateAsync<T>(T entity, CancellationToken cancellationToken) where T : Entity
  {
    if (entity is null)
    {
      throw new ArgumentNullException(nameof(entity), "Entity cannot be null");
    }

    await dbContext.Set<T>().AddAsync(entity, cancellationToken);

    return entity;
  }

  public async Task<Ledger> GetLedgerAsync(Guid id, CancellationToken cancellationToken)
  {
    if (id == Guid.Empty)
    {
      throw new ArgumentException("ID cannot be empty", nameof(id));
    }

    return await dbContext.Ledgers.AsSplitQuery()
      .Include(x => x.Budgets)
      .ThenInclude(x => x.Funds)
      .Include(x => x.Accounts)
      .ThenInclude(x => x.Incomes)
      .SingleAsync(x => x.Id == id, cancellationToken)
      ?? throw new KeyNotFoundException($"Ledger with ID {id} not found.");
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

    return await dbContext.Set<T>().AnyAsync(predicate, cancellationToken);
  }

  public async Task UpdateAsync<T>(Guid id, IEnumerable<Expression<Func<T, object>>> updatedProperties, CancellationToken cancellationToken) where T : Entity
  {
    if (id == Guid.Empty)
    {
      throw new ArgumentException("ID cannot be empty", nameof(id));
    }

    if (updatedProperties is null || !updatedProperties.Any())
    {
      throw new ArgumentException("At least one property must be specified for update", nameof(updatedProperties));
    }

    var entity = await GetAsync<T>(id, cancellationToken);

    var dbEntity = dbContext.Entry(entity);
    dbEntity.State = EntityState.Unchanged;

    foreach (var property in updatedProperties)
    {
      dbEntity.Property(property).IsModified = true;
    }
  }

  public async Task DeleteAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : Entity
  {
    if (id == Guid.Empty)
    {
      throw new ArgumentException("ID cannot be empty", nameof(id));
    }

    var entity = await GetAsync<T>(id, cancellationToken);
    dbContext.Set<T>().Remove(entity);
  }
}