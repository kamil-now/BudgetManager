using BudgetManager.Common.Models;

namespace BudgetManager.Application.Models;

public record LedgerStatisticsDTO
{
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }

    public required Dictionary<string, Statistics> TotalIncome { get; init; }
    public required Dictionary<string, Statistics> TotalExpense { get; init; }

    public required IEnumerable<ItemStatistics> Accounts { get; init; }
    public required IEnumerable<ItemStatistics> Tags { get; init; }

    public record ItemStatistics(
        string Name,
        Dictionary<string, Statistics> Income,
        Dictionary<string, Statistics> Expense
    );

    public record Statistics(
        decimal Total,
        decimal Average,
        decimal Min,
        decimal Max);

}
