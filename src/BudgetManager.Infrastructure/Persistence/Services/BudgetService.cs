using System.Linq.Expressions;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BudgetManager.Infrastructure.Persistence.Services;

public class BudgetService(ApplicationDbContext dbContext) : IBudgetService
{
  public async Task<T> Add<T>(T entity, CancellationToken cancellationToken) where T : Entity
  {
    if (entity is null)
    {
      throw new ArgumentNullException(nameof(entity), "Entity cannot be null");
    }

    dbContext.Set<T>().Add(entity);

    await dbContext.SaveChangesAsync(cancellationToken);

    return entity;
  }

  public async Task<T> Get<T>(Guid id, CancellationToken cancellationToken) where T : Entity
  {
    if (id == Guid.Empty)
    {
      throw new ArgumentException("ID cannot be empty", nameof(id));
    }

    return await dbContext.Set<T>().FindAsync([id], cancellationToken)
      ?? throw new KeyNotFoundException($"Entity of type {typeof(T).Name} with ID {id} not found.");
  }

  public async Task<IEnumerable<T>> Get<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken) where T : Entity
  {
    ArgumentNullException.ThrowIfNull(predicate);

    return await dbContext.Set<T>()
        .Where(predicate)
        .ToListAsync(cancellationToken);
  }
}