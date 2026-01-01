using BudgetManager.Domain.Entities;

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
}
