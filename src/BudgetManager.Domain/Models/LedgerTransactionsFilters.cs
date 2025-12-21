namespace BudgetManager.Domain.Models;

public record LedgerTransactionsFilters
{
  public DateTimeOffset? From { get; set; }
  public DateTimeOffset? To { get; set; }
  public Guid? AccountId { get; set; }
  public Guid? BudgetId { get; set; }
  public Guid? FundId { get; set; }
}
