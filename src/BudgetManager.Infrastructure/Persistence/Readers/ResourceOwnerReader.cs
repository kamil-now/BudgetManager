using System.Linq.Expressions;
using BudgetManager.Application.Interfaces;
using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetManager.Infrastructure.Persistence.Readers;

public sealed class ResourceOwnerReader(ApplicationDbContext dbContext) : IResourceOwnerReader
{
    public async Task<Guid?> ReadOwnerIdAsync<T>(Guid id, Expression<Func<T, Guid>> ownerSelector, CancellationToken cancellationToken = default) where T : Entity
    {
        var ownerIds = await dbContext.Set<T>()
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(ownerSelector)
            .ToArrayAsync(cancellationToken);

        return ownerIds.Cast<Guid?>().SingleOrDefault();
    }
}
