using BudgetManager.Common.Models;

namespace BudgetManager.Application.Commands;

public record UpdateIncomeCommand(
  Guid Id,
  Guid AccountId,
  Money Amount,
  DateTimeOffset Date,
  string? Title,
  string? Description = null,
  IEnumerable<string>? Tags = null
) : IRequest;
