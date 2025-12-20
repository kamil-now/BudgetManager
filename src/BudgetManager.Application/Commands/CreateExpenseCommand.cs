using BudgetManager.Common.Models;

namespace BudgetManager.Application.Commands;

public record CreateExpenseCommand(
  Guid AccountId,
  Money Amount,
  DateTimeOffset Date,
  string? Title,
  string? Description = null,
  IEnumerable<string>? Tags = null
) : CreateAccountTransactionCommand(
  AccountId,
  Amount,
  Date,
  Title,
  Description,
  Tags
), IRequest<Guid>;
