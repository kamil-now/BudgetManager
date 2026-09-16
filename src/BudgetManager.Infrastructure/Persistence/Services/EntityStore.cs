using System.Linq.Expressions;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BudgetManager.Infrastructure.Persistence.Services;

public class EntityStore(ApplicationDbContext dbContext) : IEntityStore
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConflictException(ConcurrencyMessage(ex));
        }
    }

    private static string ConcurrencyMessage(DbUpdateConcurrencyException ex)
    {
        var entry = ex.Entries.FirstOrDefault();

        if (entry?.Entity is not Entity entity)
            return "The record was modified by another request.";

        return $"{entry.Entity.GetType().Name} {entity.Id} was modified by another request.";
    }

    public async Task<T> RunInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await action();
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
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

    public async Task<T> GetAsync<T>(Guid id, CancellationToken cancellationToken) where T : Entity
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("ID cannot be empty", nameof(id));
        }

        return await dbContext.Set<T>().FindAsync([id], cancellationToken)
          ?? throw new NotFoundException($"Entity of type '{typeof(T).Name}' with ID '{id}' not found.");
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
