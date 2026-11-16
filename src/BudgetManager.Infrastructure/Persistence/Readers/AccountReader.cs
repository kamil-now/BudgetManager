using BudgetManager.Application.Interfaces;
using BudgetManager.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BudgetManager.Infrastructure.Persistence.Readers;

public sealed class AccountReader(ApplicationDbContext dbContext) : IAccountReader
{
    public async Task<AccountSummary?> ReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var account = await dbContext.Accounts
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Id, x.LedgerId, x.Name })
            .SingleOrDefaultAsync(cancellationToken);

        if (account is null)
        {
            return null;
        }

        var balanceRows = await dbContext.AccountTransactions
            .AsNoTracking()
            .Where(x => x.AccountId == id)
            .GroupBy(x => x.Value.Currency)
            .Select(g => new { Currency = g.Key, Amount = g.Sum(x => x.Value.Amount) })
            .ToArrayAsync(cancellationToken);

        var balance = new Balance();
        foreach (var row in balanceRows)
        {
            balance.Add(new Money(row.Amount, row.Currency));
        }

        return new AccountSummary(account.Id, account.LedgerId, account.Name, balance);
    }
}
