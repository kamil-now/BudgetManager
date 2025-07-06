using BudgetManager.Common.Models;

namespace BudgetManager.Application.Commands;

public abstract record CreateBudgetTransactionCommand(
  Guid BudgetId,
  Guid FundId,
  Money Amount,
  DateTimeOffset Date,
  string Title,
  string? Description = null,
  IEnumerable<string>? Tags = null
);
