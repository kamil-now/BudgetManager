using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Models;

namespace BudgetManager.Domain.Entities;

public sealed class PercentAllocationTemplateLine : AllocationTemplateLine
{
    public required decimal Percent { get; set; }

    public override AllocationRule ToRule() => new(FundId, Sequence, AllocationType.Percent, Percent);
}
