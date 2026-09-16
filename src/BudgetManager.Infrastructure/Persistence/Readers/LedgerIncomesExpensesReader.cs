using BudgetManager.Application.Interfaces;
using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetManager.Infrastructure.Persistence.Readers;

public sealed class LedgerIncomesExpensesReader(ApplicationDbContext dbContext) : ILedgerIncomesExpensesReader
{
    public async Task<IEnumerable<AccountTransaction>> ReadAsync(Guid ledgerId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken = default)
    {
        var accounts = await dbContext.Accounts
             .AsNoTracking()
             .Where(x => x.LedgerId == ledgerId)
             .Select(x => new { x.Id, x.Name })
             .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        var accountTransactions = await dbContext.AccountTransactions
                .AsNoTracking()
                .Include(x => x.InTransfer)
                .Include(x => x.OutTransfer)
                .Where(x => x.InTransfer == null && x.OutTransfer == null && accounts.Keys.Contains(x.AccountId))
                .Where(x => (from == null || x.Date >= from) && (to == null || x.Date <= to))
                .ToArrayAsync(cancellationToken);

        return accountTransactions;
    }
}
