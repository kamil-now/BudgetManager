namespace BudgetManager.Domain.Entities;

public sealed class Deallocation : BudgetTransaction
{
    public Guid? ExpenseId { get; set; }
    public Expense? Expense { get; set; } = null!;
}
