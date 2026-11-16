using BudgetManager.Application.Security;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Commands;

/// <summary>
/// Creates a named allocation template of the ledger in <see cref="Currency"/>, which cannot change afterwards.
/// The lines are the steps of the pipeline, in sequence order, each for one of the ledger's budgets.
/// </summary>
public record CreateLedgerAllocationTemplateCommand(
  Guid LedgerId,
  string Name,
  string Currency,
  IEnumerable<LedgerAllocationLine> Lines
) : IRequest<Guid>, IRequiresAccess
{
    IEnumerable<Resource> IRequiresAccess.Resources => [Resource.Of<Ledger>(LedgerId)];
}
