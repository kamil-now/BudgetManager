using BudgetManager.Application.Models;
using BudgetManager.Application.Security;

namespace BudgetManager.Application.Queries;

public record GetLedgersQuery() : IRequest<IReadOnlyList<LedgerListItemDTO>>, IRequiresAccess
{
    IEnumerable<Resource> IRequiresAccess.Resources => [];
}
