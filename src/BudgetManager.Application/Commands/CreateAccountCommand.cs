using BudgetManager.Application.Security;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Commands;

public record CreateAccountCommand(
  Guid LedgerId,
  Money InitialBalance,
  string Name
): IRequest<Guid>, IRequiresAccess
{
    IEnumerable<Resource> IRequiresAccess.Resources => [Resource.Of<Ledger>(LedgerId)];
}
