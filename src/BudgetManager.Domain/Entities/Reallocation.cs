namespace BudgetManager.Domain.Entities;

public sealed class Reallocation : BudgetTransaction
{
    public required Guid TargetFundId { get; set; }

    public Fund TargetFund { get; set; } = null!;
}
