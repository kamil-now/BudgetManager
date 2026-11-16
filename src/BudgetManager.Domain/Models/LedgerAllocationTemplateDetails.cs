using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Services;

namespace BudgetManager.Domain.Models;

public record LedgerAllocationTemplateDetails(Guid Id, string Name, string Currency, IEnumerable<LedgerAllocationTemplateDetails.Line> Lines)
{
    public record Line(Guid BudgetId, string BudgetName, int Sequence, AllocationType Type, decimal? Amount, decimal? Percent);

    /// <summary>
    /// The smallest amount that funds every fixed line of the template fully.
    /// </summary>
    public decimal MinimumAmount() => AllocationCalculator.Minimum(Lines.Select(x => new AllocationRule(
        x.BudgetId,
        x.Sequence,
        x.Type,
        (x.Type == AllocationType.Fixed ? x.Amount : x.Percent) ?? 0)));
}
