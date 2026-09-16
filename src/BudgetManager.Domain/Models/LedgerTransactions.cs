namespace BudgetManager.Domain.Models;

public record LedgerTransactions
{
  public required IEnumerable<AccountTransaction> AccountTransactions { get; init; }
  public required IEnumerable<AccountTransfer> AccountTransfers { get; init; }
  public required Dictionary<Guid, string> Accounts { get; init; }

  public required IEnumerable<FundTransaction> FundTransactions { get; init; }
  public required IEnumerable<FundTransfer> FundTransfers { get; init; }
  public required Dictionary<Guid, (Guid, string)> Funds { get; init; }

  public required Dictionary<Guid, string> Budgets { get; init; }

  public record AccountTransaction(
    Guid Id,
    Guid AccountId,
    Money Value,
    DateTimeOffset Date,
    string? Title,
    string? Comment,
    List<string>? Tags,
    bool IsTransferLeg);

  public record AccountTransfer(Guid Id, Guid IncomeId, Guid ExpenseId);

  public record FundTransaction(
    Guid Id,
    Guid FundId,
    Money Value,
    DateTimeOffset Date,
    string? Title,
    string? Comment,
    bool IsTransferLeg);

  public record FundTransfer(Guid Id, Guid AllocationId, Guid DeallocationId);
}
