using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Models;

public record LedgerFeedPageDTO(IReadOnlyList<FeedItemDTO> Items, int Total, int Offset, int Limit);

public record FeedItemDTO(
    Guid Id,
    FeedItemType Type,
    DateTimeOffset Date,
    string? Title,
    Guid? AccountId,
    string? AccountName,
    Guid? TargetAccountId,
    string? TargetAccountName,
    Guid? BudgetId,
    string? BudgetName,
    Guid? FundId,
    string? FundName,
    Money Value,
    Money? Received,
    bool IsInitialBalance,
    decimal Allocated,
    IEnumerable<FeedAllocationDTO> Allocations);

public record FeedAllocationDTO(Guid Id, Guid FundId, string FundName, Guid BudgetId, string BudgetName, Money Amount);
