namespace BudgetManager.Domain.Entities;

public sealed class FixedAllocationTemplateLine : AllocationTemplateLine
{
    public required decimal Amount { get; set; }
}
