using BudgetManager.Common.Models;

namespace BudgetManager.Application.Commands;

public record CreateDeallocationCommand(
  Guid BudgetId,
  Guid FundId,
  Guid ExpenseId,
  Money Amount,
  DateTimeOffset Date,
  string Title,
  string? Description = null,
  IEnumerable<string>? Tags = null
): CreateBudgetTransactionCommand(
  BudgetId,
  FundId,
  Amount,
  Date,
  Title,
  Description,
  Tags
), IRequest<Guid>;
