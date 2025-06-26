using BudgetManager.Common.Models;

namespace BudgetManager.Application.Commands;

public abstract record CreateTransactionCommand(
  Guid LedgerId,
  Guid AccountId,
  Money Amount,
  DateTimeOffset Date,
  string Title,
  string? Description = null,
  IEnumerable<string>? Tags = null
);
