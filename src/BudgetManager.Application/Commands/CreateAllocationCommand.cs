using BudgetManager.Common.Models;

namespace BudgetManager.Application.Commands;

public record CreateAllocationCommand(
  Guid BudgetId,
  Guid FundId,
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
