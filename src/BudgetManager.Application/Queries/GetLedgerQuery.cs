using BudgetManager.Application.Models;

namespace BudgetManager.Application.Queries;

public record GetLedgerQuery(Guid Id) : IRequest<LedgerDTO?>;
