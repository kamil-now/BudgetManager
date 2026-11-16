using BudgetManager.Application.Models;
using BudgetManager.Application.Security;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Queries;

public record GetLedgerFeedQuery(Guid LedgerId, LedgerFeedFilters Filters) : IRequest<LedgerFeedPageDTO>, IRequiresAccess
{
    IEnumerable<Resource> IRequiresAccess.Resources => [Resource.Of<Ledger>(LedgerId)];
}
