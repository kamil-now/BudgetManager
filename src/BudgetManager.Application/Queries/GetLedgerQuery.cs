using BudgetManager.Application.Models;

namespace BudgetManager.Application.Queries;

public record GetLedgerQuery() : IRequest<LedgerDTO?>;
