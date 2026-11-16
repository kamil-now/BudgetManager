using BudgetManager.Application.Models;
using BudgetManager.Application.Security;
using BudgetManager.Domain.Entities;

namespace BudgetManager.Application.Commands;

public record CreateBudgetCommand(
  Guid LedgerId,
  string Name,
  IEnumerable<CreateFundDTO> Funds,
  IEnumerable<CreateAllocationTemplateDTO>? AllocationTemplates = null
) : IRequest<Guid>, IRequiresAccess
{
    IEnumerable<Resource> IRequiresAccess.Resources => [Resource.Of<Ledger>(LedgerId)];
}
