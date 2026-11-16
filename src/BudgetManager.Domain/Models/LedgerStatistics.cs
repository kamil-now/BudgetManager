namespace BudgetManager.Domain.Models;

public record LedgerStatistics
{
    public required IEnumerable<Account> Accounts { get; init; }
    public required IEnumerable<Budget> Budgets { get; init; }
    public required IEnumerable<Aggregate> Totals { get; init; }
    public required IEnumerable<AccountAggregate> AccountTotals { get; init; }
    public required IEnumerable<FundAggregate> FundTotals { get; init; }

    public record Account(Guid Id, string Name);

    public record Budget(Guid Id, string Name, IEnumerable<Fund> Funds);

    public record Fund(Guid Id, string Name);

    public record Aggregate(bool IsIncome, string Currency, decimal Total, decimal Average, decimal Min, decimal Max);

    public record AccountAggregate(Guid AccountId, Aggregate Value);

    public record FundAggregate(Guid FundId, string Currency, decimal Allocated, decimal Spent, decimal Ending);
}
