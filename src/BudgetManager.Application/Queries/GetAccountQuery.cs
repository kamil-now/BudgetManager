using BudgetManager.Application.Models;
using BudgetManager.Application.Security;
using BudgetManager.Domain.Entities;

namespace BudgetManager.Application.Queries;

public record GetAccountQuery(Guid Id) : IRequest<AccountDTO>, IRequiresAccess
{
    IEnumerable<Resource> IRequiresAccess.Resources => [Resource.Of<Account>(Id)];
}
