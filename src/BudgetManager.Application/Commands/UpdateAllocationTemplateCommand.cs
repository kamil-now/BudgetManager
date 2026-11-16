using BudgetManager.Application.Security;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Commands;

/// <summary>
/// Renames the template and replaces its lines wholesale. Its currency stays as it is.
/// </summary>
public record UpdateAllocationTemplateCommand(
  Guid BudgetId,
  Guid TemplateId,
  string Name,
  IEnumerable<AllocationLine> Lines
) : IRequest, IRequiresAccess
{
    IEnumerable<Resource> IRequiresAccess.Resources => [Resource.Of<Budget>(BudgetId)];
}
