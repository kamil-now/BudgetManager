using BudgetManager.Application.Interfaces;
using BudgetManager.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BudgetManager.Infrastructure.Persistence.Readers;

public sealed class LedgerReader(ApplicationDbContext dbContext) : ILedgerReader
{
    private sealed record BalanceRow(Guid OwnerId, string Currency, decimal Amount);

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

        var accountBalances = ToBalances(await dbContext.AccountTransactions
            .AsNoTracking()
            .Where(x => x.Account.LedgerId == id)
            .GroupBy(x => new { x.AccountId, x.Value.Currency })
            .Select(g => new BalanceRow(g.Key.AccountId, g.Key.Currency, g.Sum(x => x.Value.Amount)))
            .ToArrayAsync(cancellationToken));

        var fundBalances = ToBalances(await dbContext.FundTransactions
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
            Accounts = [.. ledger.Accounts.Select(a => new LedgerSummary.Account(a.Id, a.Name, a.Description, BalanceOf(accountBalances, a.Id)))],
            Budgets = [.. ledger.Budgets.Select(b => new LedgerSummary.Budget(
                b.Id,
                b.Name,
                b.Description,
                [.. b.Funds.Select(f => new LedgerSummary.Fund(f.Id, f.Name, f.Description, BalanceOf(fundBalances, f.Id)))]))]
        };
    }

    private static Dictionary<Guid, Balance> ToBalances(IEnumerable<BalanceRow> rows)
    {
        var balances = new Dictionary<Guid, Balance>();
        foreach (var row in rows)
        {
            if (!balances.TryGetValue(row.OwnerId, out var balance))
            {
                balance = [];
                balances[row.OwnerId] = balance;
            }
            balance.Add(new Money(row.Amount, row.Currency));
        }
        return balances;
    }

    private static Balance BalanceOf(Dictionary<Guid, Balance> balances, Guid ownerId)
        => balances.TryGetValue(ownerId, out var balance) ? balance : [];
}
