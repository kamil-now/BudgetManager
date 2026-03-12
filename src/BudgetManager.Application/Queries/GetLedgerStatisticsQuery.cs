using BudgetManager.Application.Models;
using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Queries;

public record GetLedgerStatisticsQuery(Guid LedgerId, LedgerStatisticsFilters Filters) : IRequest<LedgerStatisticsDTO?>;