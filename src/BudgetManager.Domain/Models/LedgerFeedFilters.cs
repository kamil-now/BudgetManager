using BudgetManager.Domain.Enums;

namespace BudgetManager.Domain.Models;

/// <summary>
/// Filters and paging of a ledger's transactions feed. <see cref="AccountId"/> keeps the account-side items touching
/// that account; <see cref="BudgetId"/> and <see cref="FundId"/> keep the fund-side items in that scope plus the incomes
/// allocated into it.
/// </summary>
public record LedgerFeedFilters
{
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public FeedItemType[]? Types { get; set; }
    public string? Search { get; set; }
    public Guid? AccountId { get; set; }
    public Guid? BudgetId { get; set; }
    public Guid? FundId { get; set; }
    public int Offset { get; set; }
    public int Limit { get; set; } = Constants.DefaultFeedLimit;
}
