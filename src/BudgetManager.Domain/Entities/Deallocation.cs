namespace BudgetManager.Domain.Entities;

public sealed class Deallocation : BudgetTransaction
{
  public required Guid ExpenseId { get; set; }
  public Expense Expense { get; set; } = null!;
}
