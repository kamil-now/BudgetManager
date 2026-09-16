using BudgetManager.Application.Interfaces;
using BudgetManager.Application.Models;
using BudgetManager.Application.Queries;
using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Handlers;

public sealed class GetLedgerStatisticsHandler(ILedgerIncomesExpensesReader incomesExpensesReader) : IRequestHandler<GetLedgerStatisticsQuery, LedgerStatisticsDTO?>
{
    public async Task<LedgerStatisticsDTO?> Handle(GetLedgerStatisticsQuery query, CancellationToken cancellationToken)
    {
        var statistics = await incomesExpensesReader.ReadAsync(query.LedgerId, query.Filters, cancellationToken);
        if (statistics is null)
        {
            return null;
        }

        var accountTotals = statistics.AccountTotals.ToLookup(x => x.AccountId, x => x.Value);
        var tagTotals = statistics.TagTotals.ToLookup(x => x.Tag, x => x.Value);

        return new LedgerStatisticsDTO()
        {
            From = query.Filters.From,
            To = query.Filters.To,
            TotalIncome = CreateStatistics(statistics.Totals, isIncome: true),
            TotalExpense = CreateStatistics(statistics.Totals, isIncome: false),
            Accounts = [.. statistics.Accounts.Select(account => new LedgerStatisticsDTO.ItemStatistics(
                Name: account.Name,
                Income: CreateStatistics(accountTotals[account.Id], isIncome: true),
                Expense: CreateStatistics(accountTotals[account.Id], isIncome: false)
            ))],
            Tags = [.. tagTotals.Select(tag => new LedgerStatisticsDTO.ItemStatistics(
                Name: tag.Key,
                Income: CreateStatistics(tag, isIncome: true),
                Expense: CreateStatistics(tag, isIncome: false)
            ))]
        };
    }

    private static Dictionary<string, LedgerStatisticsDTO.Statistics> CreateStatistics(IEnumerable<LedgerStatistics.Aggregate> aggregates, bool isIncome)
        => aggregates
            .Where(x => x.IsIncome == isIncome)
            .ToDictionary(
                x => x.Currency,
                x => new LedgerStatisticsDTO.Statistics(x.Total, x.Average, x.Min, x.Max));
}
