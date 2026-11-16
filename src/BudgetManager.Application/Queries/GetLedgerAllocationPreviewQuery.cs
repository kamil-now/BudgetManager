using BudgetManager.Application.Models;
using BudgetManager.Application.Security;
using BudgetManager.Domain.Entities;

namespace BudgetManager.Application.Queries;

public record GetLedgerAllocationPreviewQuery(
  Guid LedgerId,
  decimal Amount,
  string Currency,
  AllocationInput Allocation
) : IRequest<AllocationPreviewDTO>, IRequiresAccess
{
    IEnumerable<Resource> IRequiresAccess.Resources =>
      [Resource.Of<Ledger>(LedgerId), .. Allocation?.ToResources() ?? Enumerable.Empty<Resource>()];
}
