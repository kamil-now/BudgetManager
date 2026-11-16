using BudgetManager.Domain.Enums;

namespace BudgetManager.Application.Models;

public record LedgerAllocationTemplateDTO(Guid Id, string Name, string Currency, decimal MinimumAmount, IEnumerable<LedgerAllocationTemplateDTO.Line> Lines)
{
    public record Line(Guid BudgetId, string BudgetName, int Sequence, AllocationType Type, decimal? Amount, decimal? Percent);
}
