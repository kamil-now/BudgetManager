using BudgetManager.Application.Models;
using BudgetManager.Application.Security;
using BudgetManager.Domain.Entities;

namespace BudgetManager.Application.Queries;

public record GetAccountTransactionQuery(Guid AccountId, Guid Id) : IRequest<AccountTransactionDTO>, IRequiresAccess
{
    IEnumerable<Resource> IRequiresAccess.Resources =>
        [Resource.Of<Account>(AccountId), Resource.Of<AccountTransaction>(Id)];
}
