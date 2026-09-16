using BudgetManager.Application.Interfaces;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BudgetManager.Infrastructure.Persistence.Readers;

public sealed class LedgerIncomesExpensesReader(ApplicationDbContext dbContext) : ILedgerIncomesExpensesReader
{
    public async Task<LedgerStatistics?> ReadAsync(Guid ledgerId, LedgerStatisticsFilters filters, CancellationToken cancellationToken = default)
    {
        var accounts = await dbContext.Ledgers
            .AsNoTracking()
            .Where(x => x.Id == ledgerId)
            .Select(x => x.Accounts.OrderBy(a => a.Name).Select(a => new { a.Id, a.Name }).ToList())
            .SingleOrDefaultAsync(cancellationToken);

        if (accounts is null)
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

        var tagged = await transactions
            .Where(x => x.Tags != null)
            .Select(x => new { x.Tags, x.Value.Amount, x.Value.Currency })
            .ToArrayAsync(cancellationToken);

        var tagTotals = tagged
            .SelectMany(x => x.Tags!, (row, tag) => new { Tag = tag, row.Amount, row.Currency })
            .GroupBy(x => new { x.Tag, IsIncome = x.Amount > 0, x.Currency })
            .Select(g => new LedgerStatistics.TagAggregate(
                g.Key.Tag,
                new LedgerStatistics.Aggregate(
                    g.Key.IsIncome,
                    g.Key.Currency,
                    g.Sum(x => x.Amount),
                    g.Average(x => x.Amount),
                    g.Min(x => x.Amount),
                    g.Max(x => x.Amount))))
            .ToArray();

        return new LedgerStatistics
        {
            Accounts = [.. accounts.Select(x => new LedgerStatistics.Account(x.Id, x.Name))],
            Totals = totals,
            AccountTotals = accountTotals,
            TagTotals = tagTotals
        };
    }

    private static IQueryable<AccountTransaction> Filter(IQueryable<AccountTransaction> transactions, Guid ledgerId, LedgerStatisticsFilters filters)
        => transactions
            .Where(x => x.Account.LedgerId == ledgerId)
            .Where(x => x.InTransfer == null && x.OutTransfer == null)
            .Where(x => x.Value.Amount != 0)
            .Where(x => (filters.From == null || x.Date >= filters.From) && (filters.To == null || x.Date <= filters.To))
            .Where(x => filters.IncludeInitialBalance || x.Title == null || x.Title != Constants.InitialBalanceTransactionTitle);
}
