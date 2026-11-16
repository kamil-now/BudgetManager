using BudgetManager.Application.Models;
using BudgetManager.Application.Security;
using BudgetManager.Domain.Entities;

namespace BudgetManager.Application.Commands;

/// <summary>
/// Renames the budget and sets its funds: a fund with an id is renamed, a fund without one is added, and a fund of the
/// budget that is not listed is removed. A removed fund's money is deallocated (unallocated again), its allocation
/// template lines go with it, and allocations and templates left with nothing are deleted.
/// </summary>
public record UpdateBudgetCommand(
  Guid Id,
  string Name,
  IEnumerable<UpdateFundDTO> Funds
) : IRequest, IRequiresAccess
{
    IEnumerable<Resource> IRequiresAccess.Resources => [Resource.Of<Budget>(Id)];
}
