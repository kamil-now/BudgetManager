using BudgetManager.Application.Security;
using BudgetManager.Domain.Entities;

namespace BudgetManager.Application.Commands;

/// <summary>
/// Deletes an income or expense of the account, with the fund allocations made from it.
/// </summary>
public record DeleteAccountTransactionCommand(Guid AccountId, Guid Id) : IRequest, IRequiresAccess
{
    IEnumerable<Resource> IRequiresAccess.Resources => [Resource.Of<Account>(AccountId), Resource.Of<AccountTransaction>(Id)];
}
