using BudgetManager.Common.Models;

namespace BudgetManager.Application.Commands;

public record CreateReallocationCommand(
  Guid BudgetId,
  Guid FundId,
  Guid TargetFundId,
  Money Amount,
  DateTimeOffset Date,
  string Title,
  string? Description = null,
  IEnumerable<string>? Tags = null
) : CreateBudgetTransactionCommand(
  BudgetId,
  FundId,
  Amount,
  Date,
  Title,
  Description,
  Tags
), IRequest<Guid>;
