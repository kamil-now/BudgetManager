namespace BudgetManager.Domain.Entities;

public sealed class PercentAllocationTemplateLine : AllocationTemplateLine
{
    public required decimal Percent { get; set; }
}
