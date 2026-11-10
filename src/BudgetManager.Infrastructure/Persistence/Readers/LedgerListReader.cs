using BudgetManager.Application.Interfaces;
using BudgetManager.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BudgetManager.Infrastructure.Persistence.Readers;

public sealed class LedgerListReader(ApplicationDbContext dbContext) : ILedgerListReader
{
    public async Task<IReadOnlyList<LedgerListItem>> ReadAllAsync(Guid ownerId, CancellationToken cancellationToken = default)
        => await dbContext.Ledgers
            .AsNoTracking()
            .Where(x => x.OwnerId == ownerId)
            .OrderBy(x => x.Name)
            .Select(x => new LedgerListItem(x.Id, x.Name, x.Description))
            .ToArrayAsync(cancellationToken);
}
