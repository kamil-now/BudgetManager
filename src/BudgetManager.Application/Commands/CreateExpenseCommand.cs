using BudgetManager.Common.Models;

namespace BudgetManager.Application.Commands;

public record CreateExpenseCommand(
  Guid LedgerId,
  Guid AccountId,
  Money Amount,
  DateTimeOffset Date,
  string Title,
  string? Description = null,
  IEnumerable<string>? Tags = null
): CreateTransactionCommand(
  LedgerId,
  AccountId,
  Amount,
  Date,
  Title,
  Description,
  Tags
);
