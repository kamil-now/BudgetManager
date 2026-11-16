using BudgetManager.Application.Interfaces;
using BudgetManager.Application.Models;
using BudgetManager.Application.Queries;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Handlers;

public sealed class GetLedgerStatisticsHandler(ILedgerStatisticsReader statisticsReader) : IRequestHandler<GetLedgerStatisticsQuery, LedgerStatisticsDTO?>
{
    public async Task<LedgerStatisticsDTO?> Handle(GetLedgerStatisticsQuery query, CancellationToken cancellationToken)
    {
        query.Filters.From.EnsureValidRange(query.Filters.To);

        var statistics = await statisticsReader.ReadAsync(query.LedgerId, query.Filters, cancellationToken);
        if (statistics is null)
        {
            return null;
        }

        var accountTotals = statistics.AccountTotals.ToLookup(x => x.AccountId, x => x.Value);
        var fundTotals = statistics.FundTotals.ToLookup(x => x.FundId);

        var budgets = statistics.Budgets
            .Select(budget =>
            {
                var funds = budget.Funds
                    .Select(fund => new LedgerStatisticsDTO.FundStatistics(
                        fund.Id,
                        fund.Name,
                        CreateFlows(fundTotals[fund.Id])))
                    .ToArray();

                return new LedgerStatisticsDTO.BudgetStatistics(budget.Id, budget.Name, SumFlows(funds.Select(x => x.Flows)), funds);
            })
            .ToArray();

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
            FundTotals = SumFlows(budgets.Select(x => x.Totals)),
            Budgets = budgets
        };
    }

    private static Dictionary<string, LedgerStatisticsDTO.Statistics> CreateStatistics(IEnumerable<LedgerStatistics.Aggregate> aggregates, bool isIncome)
        => aggregates
            .Where(x => x.IsIncome == isIncome)
            .ToDictionary(
                x => x.Currency,
                x => new LedgerStatisticsDTO.Statistics(x.Total, x.Average, x.Min, x.Max));

    private static Dictionary<string, LedgerStatisticsDTO.FundFlow> CreateFlows(IEnumerable<LedgerStatistics.FundAggregate> aggregates)
        => aggregates
            .Select(x => (x.Currency, Flow: new LedgerStatisticsDTO.FundFlow(x.Allocated, x.Spent, x.Allocated + x.Spent, x.Ending)))
            .Where(x => !IsEmpty(x.Flow))
            .ToDictionary(x => x.Currency, x => x.Flow);

    private static Dictionary<string, LedgerStatisticsDTO.FundFlow> SumFlows(IEnumerable<Dictionary<string, LedgerStatisticsDTO.FundFlow>> flows)
        => flows
            .SelectMany(x => x)
            .GroupBy(x => x.Key)
            .Select(g => (Currency: g.Key, Flow: new LedgerStatisticsDTO.FundFlow(
                g.Sum(x => x.Value.Allocated),
                g.Sum(x => x.Value.Spent),
                g.Sum(x => x.Value.NetChange),
                g.Sum(x => x.Value.Ending))))
            .Where(x => !IsEmpty(x.Flow))
            .ToDictionary(x => x.Currency, x => x.Flow);

    private static bool IsEmpty(LedgerStatisticsDTO.FundFlow flow)
        => flow is { Allocated: 0, Spent: 0, NetChange: 0, Ending: 0 };
}
