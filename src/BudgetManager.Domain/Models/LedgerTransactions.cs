using BudgetManager.Domain.Entities;

namespace BudgetManager.Domain.Models;

public record LedgerTransactions
{
  public required IEnumerable<Income> Incomes { get; init; }
  public required IEnumerable<Expense> Expenses { get; init; }
  public required IEnumerable<Transfer> Transfers { get; init; }
  public required Dictionary<Guid, string> Accounts { get; init; }

  public required IEnumerable<Allocation> Allocations { get; init; }
  public required IEnumerable<Deallocation> Deallocations { get; init; }
  public required IEnumerable<Reallocation> Reallocations { get; init; }

  public required Dictionary<Guid, string> Budgets { get; init; }
  public required Dictionary<Guid, (Guid, string)> Funds { get; init; }
}
