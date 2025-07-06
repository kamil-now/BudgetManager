using BudgetManager.Common.Models;

namespace BudgetManager.Application.Commands;

public record CreateIncomeCommand(
  Guid AccountId,
  Money Amount,
  DateTimeOffset Date,
  string Title,
  string? Description = null,
  IEnumerable<string>? Tags = null
): CreateTransactionCommand(
  AccountId,
  Amount,
  Date,
  Title,
  Description,
  Tags
);
