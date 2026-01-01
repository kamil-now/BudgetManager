using BudgetManager.Common.Models;

namespace BudgetManager.Application.Commands;

public record UpdateAccountTransactionCommand(
  Guid Id,
  Guid AccountId,
  Money Amount,
  DateTimeOffset Date,
  string? Title,
  string? Comment = null,
  IEnumerable<string>? Tags = null
) : IRequest;
