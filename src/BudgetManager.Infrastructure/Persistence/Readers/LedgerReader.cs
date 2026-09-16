using BudgetManager.Application.Interfaces;
using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetManager.Infrastructure.Persistence.Readers;

public sealed class LedgerReader(ApplicationDbContext dbContext) : ILedgerReader
{
    public async Task<Ledger?> ReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Ledgers
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Budgets)
            .ThenInclude(x => x.Funds)
            .Include(x => x.Accounts.OrderBy(a => a.Name))
            .ThenInclude(x => x.Transactions)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
