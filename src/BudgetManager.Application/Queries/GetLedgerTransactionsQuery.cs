using BudgetManager.Application.Models;
using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Queries;

public record GetLedgerTransactionsQuery(Guid LedgerId, LedgerTransactionsFilters Filters) : IRequest<LedgerTransactionsDTO?>;