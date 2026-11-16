using BudgetManager.Application.Security;
using BudgetManager.Domain.Entities;

namespace BudgetManager.Application.Commands;

/// <summary>
/// Deletes an allocation of the ledger with all of its fund transactions. <see cref="Id"/> is the allocation id as the feed
/// gives it; the allocations made from an income are changed through the income.
/// </summary>
public record DeleteAllocationCommand(Guid LedgerId, Guid Id) : IRequest, IRequiresAccess
{
    IEnumerable<Resource> IRequiresAccess.Resources => [Resource.Of<Ledger>(LedgerId), Resource.Of<Allocation>(Id)];
}
