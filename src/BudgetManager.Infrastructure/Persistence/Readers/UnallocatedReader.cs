using BudgetManager.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BudgetManager.Infrastructure.Persistence.Readers;

public sealed class UnallocatedReader(ApplicationDbContext dbContext) : IUnallocatedReader
{
    public async Task<decimal> ReadAsync(Guid ledgerId, string currency, CancellationToken cancellationToken = default)
    {
        var accounts = await dbContext.AccountTransactions
            .AsNoTracking()
            .Where(x => x.Account.LedgerId == ledgerId && x.Value.Currency == currency)
            .SumAsync(x => x.Value.Amount, cancellationToken);

        var funds = await dbContext.FundTransactions
            .AsNoTracking()
            .Where(x => x.Fund.Budget.LedgerId == ledgerId && x.Value.Currency == currency)
            .SumAsync(x => x.Value.Amount, cancellationToken);

        return accounts - funds;
    }
}
