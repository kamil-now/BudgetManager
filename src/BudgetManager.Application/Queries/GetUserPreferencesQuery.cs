using BudgetManager.Application.Models;
using BudgetManager.Application.Security;

namespace BudgetManager.Application.Queries;

public record GetUserPreferencesQuery() : IRequest<UserPreferencesDTO>, IRequiresAccess
{
    IEnumerable<Resource> IRequiresAccess.Resources => [];
}
