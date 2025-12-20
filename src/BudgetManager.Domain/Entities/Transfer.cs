namespace BudgetManager.Domain.Entities;

public sealed class Transfer : AccountTransaction
{
    public required Guid TargetAccountId { get; set; }

    public Account TargetAccount { get; set; } = null!;
}
