using BudgetManager.Application.Interfaces;
using BudgetManager.Application.Models;
using BudgetManager.Application.Queries;
using BudgetManager.Domain.Models;
using BudgetManager.Domain;

namespace BudgetManager.Application.Handlers;

public sealed class GetLedgerStatisticsHandler(ILedgerReader ledgerReader, ILedgerIncomesExpensesReader incomesExpensesReader) : IRequestHandler<GetLedgerStatisticsQuery, LedgerStatisticsDTO?>
{
    public async Task<LedgerStatisticsDTO?> Handle(GetLedgerStatisticsQuery query, CancellationToken cancellationToken)
    {
        var ledger = await ledgerReader.ReadAsync(query.LedgerId, cancellationToken);
        if (ledger is null)
        {
            return null;
        }
        var transactions = await incomesExpensesReader.ReadAsync(query.LedgerId, query.Filters.From, query.Filters.To, cancellationToken);

        var incomes = transactions.Where(x => x.Value.Amount > 0);
        var expenses = transactions.Where(x => x.Value.Amount < 0);

        if (!query.Filters.IncludeInitialBalance)
        {
            incomes = incomes.Where(x => x.Title != Constants.InitialBalanceTransactionTitle);
            expenses = expenses.Where(x => x.Title != Constants.InitialBalanceTransactionTitle);
        }

        return new LedgerStatisticsDTO()
        {
            From = query.Filters.From,
            To = query.Filters.To,
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
