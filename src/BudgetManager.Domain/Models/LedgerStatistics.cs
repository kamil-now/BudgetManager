namespace BudgetManager.Domain.Models;

public record LedgerStatistics
{
  public required IEnumerable<Account> Accounts { get; init; }
  public required IEnumerable<Aggregate> Totals { get; init; }
  public required IEnumerable<AccountAggregate> AccountTotals { get; init; }
  public required IEnumerable<TagAggregate> TagTotals { get; init; }

  public record Account(Guid Id, string Name);

  public record Aggregate(bool IsIncome, string Currency, decimal Total, decimal Average, decimal Min, decimal Max);

  public record AccountAggregate(Guid AccountId, Aggregate Value);

  public record TagAggregate(string Tag, Aggregate Value);
}
