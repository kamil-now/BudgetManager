using BudgetManager.Application.Models;
using BudgetManager.Application.Security;
using BudgetManager.Domain.Entities;

namespace BudgetManager.Application.Commands;

/// <summary>
/// Allocates the ledger's unallocated money in <see cref="Currency"/> to funds: <see cref="Amount"/> of it,
/// or all of it when no amount is given. Returns the id of the created allocation.
/// </summary>
public record AllocateFundsCommand(
  Guid LedgerId,
  string Currency,
  AllocationInput Allocation,
  decimal? Amount = null,
  DateTimeOffset? Date = null
) : IRequest<Guid>, IRequiresAccess
{
    IEnumerable<Resource> IRequiresAccess.Resources =>
      [Resource.Of<Ledger>(LedgerId), .. Allocation?.ToResources() ?? Enumerable.Empty<Resource>()];
}
