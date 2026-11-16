using BudgetManager.Application.Security;
using BudgetManager.Domain.Entities;

namespace BudgetManager.Application.Commands;

/// <summary>
/// Deletes a transfer or currency exchange touching the account, with both of its transactions.
/// </summary>
public record DeleteAccountTransferCommand(Guid AccountId, Guid Id) : IRequest, IRequiresAccess
{
    IEnumerable<Resource> IRequiresAccess.Resources => [Resource.Of<Account>(AccountId), Resource.Of<AccountTransfer>(Id)];
}
