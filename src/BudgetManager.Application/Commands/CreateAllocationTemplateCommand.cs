using BudgetManager.Application.Security;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Commands;

/// <summary>
/// Creates a named allocation template of the budget in <see cref="Currency"/>, which cannot change afterwards.
/// The lines are the steps of the pipeline, in sequence order.
/// </summary>
public record CreateAllocationTemplateCommand(
  Guid BudgetId,
  string Name,
  string Currency,
  IEnumerable<AllocationLine> Lines
) : IRequest<Guid>, IRequiresAccess
{
    IEnumerable<Resource> IRequiresAccess.Resources => [Resource.Of<Budget>(BudgetId)];
}
