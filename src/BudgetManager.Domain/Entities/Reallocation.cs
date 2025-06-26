namespace BudgetManager.Domain.Entities;

public sealed class Reallocation : BudgetOperation
{
    public required Guid TargetFundId { get; init; }
}
