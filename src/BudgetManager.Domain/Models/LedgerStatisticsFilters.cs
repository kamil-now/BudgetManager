namespace BudgetManager.Domain.Models;

public record LedgerStatisticsFilters
{
  public bool IncludeInitialBalance { get; set; }
  public DateTimeOffset? From { get; set; }
  public DateTimeOffset? To { get; set; }
}
