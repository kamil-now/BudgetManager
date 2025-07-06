using BudgetManager.Application.Models;

namespace BudgetManager.Application.Queries;

public record GetFundsQuery(Guid BudgetId) : IRequest<IEnumerable<FundDTO>>;
