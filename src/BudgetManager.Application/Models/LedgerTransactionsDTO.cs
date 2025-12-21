using BudgetManager.Common.Models;

namespace BudgetManager.Application.Models;

public record LedgerTransactionsDTO
{
    public required IEnumerable<AccountTransaction> Incomes { get; init; }
    public required IEnumerable<AccountTransaction> Expenses { get; init; }
    public required IEnumerable<AccountTransaction> Transfers { get; init; }

    public required IEnumerable<BudgetTransaction> Allocations { get; init; }
    public required IEnumerable<BudgetTransaction> Deallocations { get; init; }
    public required IEnumerable<BudgetTransaction> Reallocations { get; init; }

    public record AccountTransaction(
        Guid Id,
        Guid AccountId,
        string AccountName,
        Money Amount,
        DateTimeOffset Date,
        Guid? TargetAccountId,
        string? TargetAccountName,
        string? Title,
        string? Description,
        IEnumerable<string>? Tags);

    public record BudgetTransaction(
        Guid Id,
        Guid BudgetId,
        string BudgetName,
        Guid FundId,
        string FundName,
        Money Amount,
        DateTimeOffset Date,
        Guid? TargetFundId,
        string? TargetFundName,
        string? Title,
        string? Description);

}
