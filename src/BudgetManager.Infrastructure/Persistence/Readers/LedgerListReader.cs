using BudgetManager.Application.Interfaces;
using BudgetManager.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BudgetManager.Infrastructure.Persistence.Readers;

public sealed class LedgerListReader(ApplicationDbContext dbContext) : ILedgerListReader
{
    public async Task<IReadOnlyList<LedgerListItem>> ReadAllAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        var ledgers = await dbContext.Ledgers
            .AsNoTracking()
            .Where(x => x.OwnerId == ownerId)
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name })
            .ToArrayAsync(cancellationToken);

        var totals = (await dbContext.AccountTransactions
                .AsNoTracking()
                .Where(x => x.Account.Ledger.OwnerId == ownerId)
                .GroupBy(x => new { x.Account.LedgerId, x.Value.Currency })
                .Select(g => new { g.Key.LedgerId, g.Key.Currency, Amount = g.Sum(x => x.Value.Amount) })
                .ToArrayAsync(cancellationToken))
            .ToLookup(x => x.LedgerId);

        return [.. ledgers.Select(x => new LedgerListItem(
            x.Id,
            x.Name,
            new Balance(totals[x.Id].ToDictionary(t => t.Currency, t => t.Amount))))];
    }
}
