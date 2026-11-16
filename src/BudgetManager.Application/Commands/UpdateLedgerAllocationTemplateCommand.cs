using BudgetManager.Application.Security;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Commands;

/// <summary>
/// Renames the ledger's allocation template and replaces its lines wholesale. Its currency stays as it is.
/// </summary>
public record UpdateLedgerAllocationTemplateCommand(
  Guid LedgerId,
  Guid TemplateId,
  string Name,
  IEnumerable<LedgerAllocationLine> Lines
) : IRequest, IRequiresAccess
{
    IEnumerable<Resource> IRequiresAccess.Resources => [Resource.Of<Ledger>(LedgerId)];
}
