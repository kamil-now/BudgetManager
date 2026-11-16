using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Models;

public record AllocationLineInput(Guid FundId, int Sequence, AllocationType Type, decimal? Amount = null, decimal? Percent = null)
{
    public AllocationLine ToLine() => new(FundId, Sequence, Type, Amount, Percent);

    public AllocationRule ToRule() => new(FundId, Sequence, Type, (Type == AllocationType.Fixed ? Amount : Percent) ?? 0);
}
