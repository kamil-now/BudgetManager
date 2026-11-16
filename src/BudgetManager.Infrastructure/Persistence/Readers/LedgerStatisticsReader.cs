using BudgetManager.Application.Interfaces;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BudgetManager.Infrastructure.Persistence.Readers;

public sealed class LedgerStatisticsReader(ApplicationDbContext dbContext) : ILedgerStatisticsReader
{
    public async Task<LedgerStatistics?> ReadAsync(Guid ledgerId, LedgerStatisticsFilters filters, CancellationToken cancellationToken = default)
    {
        var structure = await dbContext.Ledgers
            .AsNoTracking()
            .Where(x => x.Id == ledgerId)
            .Select(x => new
            {
                Accounts = x.Accounts
                    .OrderBy(a => a.Name)
                    .Select(a => new LedgerStatistics.Account(a.Id, a.Name))
                    .ToList(),
                Budgets = x.Budgets
                    .OrderBy(b => b.Name)
                    .Select(b => new
                    {
                        b.Id,
                        b.Name,
                        Funds = b.Funds
                            .OrderBy(f => f.Name)
                            .Select(f => new LedgerStatistics.Fund(f.Id, f.Name))
                            .ToList()
                    })
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (structure is null)
        {
            return null;
        }

        var transactions = Filter(dbContext.AccountTransactions.AsNoTracking(), ledgerId, filters);

        var totals = await transactions
            .GroupBy(x => new { IsIncome = x.Value.Amount > 0, x.Value.Currency })
            .Select(g => new LedgerStatistics.Aggregate(
                g.Key.IsIncome,
                g.Key.Currency,
                g.Sum(x => x.Value.Amount),
                g.Average(x => x.Value.Amount),
                g.Min(x => x.Value.Amount),
                g.Max(x => x.Value.Amount)))
            .ToArrayAsync(cancellationToken);

        var accountTotals = await transactions
            .GroupBy(x => new { x.AccountId, IsIncome = x.Value.Amount > 0, x.Value.Currency })
            .Select(g => new LedgerStatistics.AccountAggregate(
                g.Key.AccountId,
                new LedgerStatistics.Aggregate(
                    g.Key.IsIncome,
                    g.Key.Currency,
                    g.Sum(x => x.Value.Amount),
                    g.Average(x => x.Value.Amount),
                    g.Min(x => x.Value.Amount),
                    g.Max(x => x.Value.Amount))))
            .ToArrayAsync(cancellationToken);

        var from = filters.From;
        var to = filters.To;
        var includeInitial = filters.IncludeInitialBalance;

        var fundTotals = await dbContext.FundTransactions
            .AsNoTracking()
            .Where(x => x.Fund.Budget.LedgerId == ledgerId && x.Value.Amount != 0)
            .Where(x => to == null || x.Date <= to)
            .GroupBy(x => new { x.FundId, x.Value.Currency })
            .Select(g => new LedgerStatistics.FundAggregate(
                g.Key.FundId,
                g.Key.Currency,
                g.Sum(x => (from == null || x.Date >= from)
                    && (includeInitial || x.Allocation == null || !x.Allocation.IsInitial)
                    && x.Value.Amount > 0 ? x.Value.Amount : 0),
                g.Sum(x => (from == null || x.Date >= from) && x.Value.Amount < 0 ? x.Value.Amount : 0),
                g.Sum(x => x.Value.Amount)))
            .ToArrayAsync(cancellationToken);

        return new LedgerStatistics
        {
            Accounts = structure.Accounts,
            Budgets = [.. structure.Budgets.Select(b => new LedgerStatistics.Budget(b.Id, b.Name, b.Funds))],
            Totals = totals,
            AccountTotals = accountTotals,
            FundTotals = fundTotals
        };
    }

    private static IQueryable<AccountTransaction> Filter(IQueryable<AccountTransaction> transactions, Guid ledgerId, LedgerStatisticsFilters filters)
        => transactions
            .Where(x => x.Account.LedgerId == ledgerId)
            .Where(x => x.OutTransfer == null || x.OutTransfer.Income.Account.LedgerId != ledgerId)
            .Where(x => x.InTransfer == null || x.InTransfer.Expense.Account.LedgerId != ledgerId)
            .Where(x => x.Value.Amount != 0)
            .Where(x => (filters.From == null || x.Date >= filters.From) && (filters.To == null || x.Date <= filters.To))
            .Where(x => filters.IncludeInitialBalance || x.Type != AccountTransactionType.InitialBalance);
}
