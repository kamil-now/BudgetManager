using BudgetManager.Application.Models;
using BudgetManager.Application.Security;

namespace BudgetManager.Application.Commands;

/// <summary>
/// Creates a ledger whose positive initial balances are fully allocated to funds by <see cref="InitialAllocations"/>.
/// </summary>
public record CreateLedgerCommand(
  string Name,
  IEnumerable<CreateBudgetDTO> Budgets,
  IEnumerable<CreateAccountDTO> Accounts,
  IEnumerable<InitialAllocationDTO>? InitialAllocations = null
) : IRequest<Guid>, IRequiresAccess
{
    IEnumerable<Resource> IRequiresAccess.Resources => [];
}
