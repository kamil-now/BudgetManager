using BudgetManager.Application.Models;
using BudgetManager.Application.Security;
using BudgetManager.Domain.Entities;

namespace BudgetManager.Application.Commands;

/// <summary>
/// Replaces an allocation of the ledger: <see cref="Amount"/> of the unallocated money in <see cref="Currency"/>
/// (plus what the allocation itself holds in that currency) is split again as <see cref="Allocation"/> asks.
/// <see cref="Id"/> is the allocation id as the feed gives it. The date of an initial allocation cannot be changed.
/// </summary>
public record UpdateAllocationCommand(
  Guid LedgerId,
  Guid Id,
  string Currency,
  decimal Amount,
  AllocationInput Allocation,
  DateTimeOffset Date
) : IRequest, IRequiresAccess
{
    IEnumerable<Resource> IRequiresAccess.Resources =>
      [
        Resource.Of<Ledger>(LedgerId),
        Resource.Of<Domain.Entities.Allocation>(Id),
        .. Allocation?.ToResources() ?? Enumerable.Empty<Resource>()
      ];
}
