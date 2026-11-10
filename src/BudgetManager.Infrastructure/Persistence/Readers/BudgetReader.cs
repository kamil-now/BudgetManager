using BudgetManager.Application.Interfaces;
using BudgetManager.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BudgetManager.Infrastructure.Persistence.Readers;

public sealed class BudgetReader(ApplicationDbContext dbContext) : IBudgetReader
{
    public async Task<BudgetSummary?> ReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var budget = await dbContext.Budgets
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Description,
                Funds = x.Funds.Select(f => new { f.Id, f.Name, f.Description }).ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (budget is null)
        {
            return null;
        }

        var fundBalances = BalanceRows.ToBalances(await dbContext.FundTransactions
            .AsNoTracking()
            .Where(x => x.Fund.BudgetId == id)
            .GroupBy(x => new { x.FundId, x.Value.Currency })
            .Select(g => new BalanceRow(g.Key.FundId, g.Key.Currency, g.Sum(x => x.Value.Amount)))
            .ToArrayAsync(cancellationToken));

        return new BudgetSummary(
            budget.Id,
            budget.Name,
            budget.Description,
            [.. budget.Funds.Select(f => new BudgetSummary.Fund(f.Id, f.Name, f.Description, BalanceRows.BalanceOf(fundBalances, f.Id)))]);
    }
}
