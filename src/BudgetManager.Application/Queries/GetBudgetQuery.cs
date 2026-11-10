using BudgetManager.Application.Models;
using BudgetManager.Application.Security;
using BudgetManager.Domain.Entities;

namespace BudgetManager.Application.Queries;

public record GetBudgetQuery(Guid Id) : IRequest<BudgetDTO>, IRequiresAccess
{
    IEnumerable<Resource> IRequiresAccess.Resources => [Resource.Of<Budget>(Id)];
}
