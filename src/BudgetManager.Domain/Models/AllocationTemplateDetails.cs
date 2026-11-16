using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Services;

namespace BudgetManager.Domain.Models;

public record AllocationTemplateDetails(Guid Id, string Name, string Currency, IEnumerable<AllocationTemplateDetails.Line> Lines)
{
    public record Line(Guid FundId, string FundName, int Sequence, AllocationType Type, decimal? Amount, decimal? Percent);

    /// <summary>
    /// The smallest amount that funds every fixed line of the template fully.
    /// </summary>
    public decimal MinimumAmount() => AllocationCalculator.Minimum(Lines.Select(x => new AllocationRule(
        x.FundId,
        x.Sequence,
        x.Type,
        (x.Type == AllocationType.Fixed ? x.Amount : x.Percent) ?? 0)));
}
