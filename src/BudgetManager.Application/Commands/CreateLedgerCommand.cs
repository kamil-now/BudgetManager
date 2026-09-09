using BudgetManager.Application.Models;
using BudgetManager.Application.Security;

namespace BudgetManager.Application.Commands;

public record CreateLedgerCommand(
  string Name,
  string? Description,
  CreateBudgetDTO Budget,
  IEnumerable<CreateAccountDTO> Accounts
) : IRequest<Guid>, IRequiresAccess
{
    IEnumerable<Resource> IRequiresAccess.Resources => [];
}
