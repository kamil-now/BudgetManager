namespace BudgetManager.Domain.Entities;

public sealed class AccountTransfer : Entity
{
    public required Guid IncomeId { get; set; }
    public required Guid ExpenseId { get; set; }

    public AccountTransaction Income { get; set; } = null!;
    public AccountTransaction Expense { get; set; } = null!;
}
