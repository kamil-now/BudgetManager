using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Models;

namespace BudgetManager.Domain.Entities;

public sealed class FixedAllocationTemplateLine : AllocationTemplateLine
{
    public required decimal Amount { get; set; }

    public override AllocationRule ToRule() => new(FundId, Sequence, AllocationType.Fixed, Amount);
}
