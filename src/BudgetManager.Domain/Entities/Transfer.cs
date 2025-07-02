namespace BudgetManager.Domain.Entities;

public sealed class Transfer : Transaction
{
    public required Guid TargetAccountId { get; set; }

    public Account TargetAccount { get; set; } = null!;
}
