using BudgetManager.Application.Security;
using BudgetManager.Domain.Entities;

namespace BudgetManager.Application.Commands;

/// <summary>
/// Renames the account. Its balance is changed through its transactions.
/// </summary>
public record UpdateAccountCommand(
  Guid Id,
  string Name
): IRequest, IRequiresAccess
{
    IEnumerable<Resource> IRequiresAccess.Resources => [Resource.Of<Account>(Id)];
}
