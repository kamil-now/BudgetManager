using BudgetManager.Common.Models;

namespace BudgetManager.Application.Commands;

public abstract record CreateAccountTransactionCommand(
  Guid AccountId,
  Money Amount,
  DateTimeOffset Date,
  string? Title,
  string? Description = null,
  IEnumerable<string>? Tags = null
) : IRequest<Guid>;
