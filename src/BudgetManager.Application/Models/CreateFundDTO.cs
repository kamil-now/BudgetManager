using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Models;

public record CreateFundDTO(
  string Name,
  IEnumerable<FundAllocation> AllocationTemplates,
  string? Description = null)
{
    public IEnumerable<FundAllocation> AllocationTemplates { get; init; } = AllocationTemplates ?? [];
}
