using BudgetManager.Application.Models;

namespace BudgetManager.Application.Queries;

public record GetLedgerStatisticsQuery(Guid LedgerId, DateTimeOffset? From, DateTimeOffset? To) : IRequest<LedgerStatisticsDTO?>;