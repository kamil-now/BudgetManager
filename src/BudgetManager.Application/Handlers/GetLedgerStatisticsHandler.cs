using BudgetManager.Application.Models;
using BudgetManager.Application.Queries;
using BudgetManager.Common.Models;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class GetLedgerStatisticsHandler(IBudgetManagerService service) : IRequestHandler<GetLedgerStatisticsQuery, LedgerStatisticsDTO?>
{
    public async Task<LedgerStatisticsDTO?> Handle(GetLedgerStatisticsQuery query, CancellationToken cancellationToken)
    {
        var ledger = await service.GetLedgerAsync(x => x.Id == query.LedgerId, cancellationToken);
        if (ledger is null)
        {
            return null;
        }
        var transactions = await service.GetLedgerIncomesExpensesAsync(query.LedgerId, query.From, query.To, cancellationToken);

        var incomes = transactions.Where(x => x.Value.Amount > 0);
        var expenses = transactions.Where(x => x.Value.Amount < 0);

        return new LedgerStatisticsDTO()
        {
            From = query.From,
            To = query.To,
            TotalIncome = CreateStatistics(incomes.Select(x => x.Value)),
            TotalExpense = CreateStatistics(expenses.Select(x => x.Value)),
            Accounts = [.. ledger.Accounts.Select(account =>
            {
                var accountIncomes = incomes.Where(x => x.AccountId == account.Id).Select(x => x.Value).ToArray();
                var accountExpenses = expenses.Where(x => x.AccountId == account.Id).Select(x => x.Value).ToArray();
                return new LedgerStatisticsDTO.ItemStatistics(
                    Name: account.Name,
                    Income: CreateStatistics(accountIncomes),
                    Expense: CreateStatistics(accountExpenses)
                );
            })],
            Tags = [.. incomes.SelectMany(x => x.Tags ?? []).Concat(expenses.SelectMany(x => x.Tags ?? [])).Distinct().Select(tag =>
            {
                var tagIncomes = incomes.Where(x => x.Tags?.Contains(tag) ?? false).Select(x => x.Value).ToArray();
                var tagExpenses = expenses.Where(x => x.Tags?.Contains(tag) ?? false).Select(x => x.Value).ToArray();
                return new LedgerStatisticsDTO.ItemStatistics(
                    Name: tag,
                    Income: CreateStatistics(tagIncomes),
                    Expense: CreateStatistics(tagExpenses)
                );
            })]
        };
    }

    private static Dictionary<string, LedgerStatisticsDTO.Statistics> CreateStatistics(IEnumerable<Money> values)
        => values
            .GroupBy(m => m.Currency)
            .ToDictionary(
                g => g.Key,
                g => new LedgerStatisticsDTO.Statistics(

                    Total: g.Sum(m => m.Amount),
                    Average: g.Average(m => m.Amount),
                    Min: g.Min(m => m.Amount),
                    Max: g.Max(m => m.Amount)
                )
            );
}
