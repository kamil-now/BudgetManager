using BudgetManager.Application.Models;
using BudgetManager.Application.Security;
using BudgetManager.Domain.Entities;

namespace BudgetManager.Application.Queries;

public record GetAccountTransferQuery(Guid AccountId, Guid Id) : IRequest<AccountTransactionDTO>, IRequiresAccess
{
    IEnumerable<Resource> IRequiresAccess.Resources =>
        [Resource.Of<AccountTransfer>(Id), Resource.Of<Account>(AccountId)];
}
