using BudgetManager.Application.Models;
using BudgetManager.Application.Security;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Queries;

public record GetLedgerStatisticsQuery(Guid LedgerId, LedgerStatisticsFilters Filters) : IRequest<LedgerStatisticsDTO?>, IRequiresAccess
{
  IEnumerable<Resource> IRequiresAccess.Resources => [Resource.Of<Ledger>(LedgerId)];
}
