using BudgetManager.Common.Models;

namespace BudgetManager.Application.Models;

public record LedgerTransactionsDTO
{
    public required IEnumerable<AccountTransaction> Incomes { get; init; }
    public required IEnumerable<AccountTransaction> Expenses { get; init; }
    public required IEnumerable<AccountTransaction> Transfers { get; init; }
    public required IEnumerable<AccountTransaction> CurrencyExchanges { get; init; }

    public required IEnumerable<FundTransaction> Allocations { get; init; }
    public required IEnumerable<FundTransaction> Deallocations { get; init; }
    public required IEnumerable<FundTransaction> Reallocations { get; init; }

    public record AccountTransaction(
        Guid Id,
        Guid AccountId,
        string AccountName,
        Money Value,
        Money? Received,
        DateTimeOffset Date,
        Guid? TargetAccountId,
        string? TargetAccountName,
        string? Title,
        string? Comment,
        IEnumerable<string>? Tags);

    public record FundTransaction(
        Guid Id,
        Guid BudgetId,
        string BudgetName,
        Guid FundId,
        string FundName,
        Money Value,
        Money? Received,
        DateTimeOffset Date,
        Guid? TargetFundId,
        string? TargetFundName,
        string? Title,
        string? Comment);

}
