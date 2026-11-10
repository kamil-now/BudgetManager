using BudgetManager.Application.Interfaces;
using BudgetManager.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BudgetManager.Infrastructure.Persistence.Readers;

public sealed class LedgerReader(ApplicationDbContext dbContext) : ILedgerReader
{
    public async Task<LedgerSummary?> ReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ledger = await dbContext.Ledgers
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Description,
                Accounts = x.Accounts
                    .OrderBy(a => a.Name)
                    .Select(a => new { a.Id, a.Name, a.Description })
                    .ToList(),
                Budgets = x.Budgets
                    .Select(b => new
                    {
                        b.Id,
                        b.Name,
                        b.Description,
                        Funds = b.Funds.Select(f => new { f.Id, f.Name, f.Description }).ToList()
                    })
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (ledger is null)
        {
            return null;
        }

        var accountBalances = BalanceRows.ToBalances(await dbContext.AccountTransactions
            .AsNoTracking()
            .Where(x => x.Account.LedgerId == id)
            .GroupBy(x => new { x.AccountId, x.Value.Currency })
            .Select(g => new BalanceRow(g.Key.AccountId, g.Key.Currency, g.Sum(x => x.Value.Amount)))
            .ToArrayAsync(cancellationToken));

        var fundBalances = BalanceRows.ToBalances(await dbContext.FundTransactions
            .AsNoTracking()
            .Where(x => x.Fund.Budget.LedgerId == id)
            .GroupBy(x => new { x.FundId, x.Value.Currency })
            .Select(g => new BalanceRow(g.Key.FundId, g.Key.Currency, g.Sum(x => x.Value.Amount)))
            .ToArrayAsync(cancellationToken));

        return new LedgerSummary
        {
            Id = ledger.Id,
            Name = ledger.Name,
            Description = ledger.Description,
            Accounts = [.. ledger.Accounts.Select(a => new LedgerSummary.Account(a.Id, a.Name, a.Description, BalanceRows.BalanceOf(accountBalances, a.Id)))],
            Budgets = [.. ledger.Budgets.Select(b => new LedgerSummary.Budget(
                b.Id,
                b.Name,
                b.Description,
                [.. b.Funds.Select(f => new LedgerSummary.Fund(f.Id, f.Name, f.Description, BalanceRows.BalanceOf(fundBalances, f.Id)))]))]
        };
    }
}
