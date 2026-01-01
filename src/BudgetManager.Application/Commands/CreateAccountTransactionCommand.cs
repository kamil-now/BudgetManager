using BudgetManager.Common.Models;

namespace BudgetManager.Application.Commands;

public record CreateAccountTransactionCommand(
  Guid AccountId,
  Money Value,
  DateTimeOffset Date,
  string? Title,
  string? Comment = null,
  IEnumerable<string>? Tags = null
) : IRequest<Guid>;
