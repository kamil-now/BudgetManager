using BudgetManager.Domain.Enums;

namespace BudgetManager.Application.Models;

public record AllocationTemplateDTO(Guid Id, string Name, string Currency, decimal MinimumAmount, IEnumerable<AllocationTemplateDTO.Line> Lines)
{
    public record Line(Guid FundId, string FundName, int Sequence, AllocationType Type, decimal? Amount, decimal? Percent);
}
