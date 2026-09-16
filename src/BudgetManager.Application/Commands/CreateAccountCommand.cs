using BudgetManager.Application.Security;
using BudgetManager.Domain.Models;
using BudgetManager.Domain.Entities;

namespace BudgetManager.Application.Commands;

public record CreateAccountCommand(
  Guid LedgerId,
  Money InitialBalance,
  string Name,
  string? Description = null
) : IRequest<Guid>, IRequiresAccess
{
    IEnumerable<Resource> IRequiresAccess.Resources => [Resource.Of<Ledger>(LedgerId)];
}
