using BudgetManager.Domain.Enums;

namespace BudgetManager.Domain.Models;

public record LedgerFeed(IReadOnlyList<LedgerFeed.Item> Items, int Total)
{
    /// <summary>
    /// An income, expense, transfer, exchange or allocation. A transfer or exchange is its AccountTransfer:
    /// the account is the source, the target account the destination, <see cref="Value"/> the positive amount sent and
    /// <see cref="Received"/> what an exchange bought. An expense paid from a fund carries the budget and fund.
    /// An allocation is its Allocation: <see cref="Value"/> is the total, the budget and fund hold the distinct names of its lines.
    /// <see cref="Allocations"/> are the fund allocations made from an income, or the lines of an allocation.
    /// <see cref="IsInitialBalance"/> marks an opening row: an initial balance or an initial allocation.
    /// </summary>
    public record Item(
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
        IReadOnlyList<Allocation> Allocations);

    public record Allocation(Guid Id, Guid FundId, string FundName, Guid BudgetId, string BudgetName, Money Amount);
}
