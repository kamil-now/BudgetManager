using BudgetManager.Common.Models;

namespace BudgetManager.Application.Commands;

public record CreateCurrencyExchangeCommand(
  Guid AccountId,
  Guid? TargetAccountId,
  Money Buy,
  Money Sell,
  DateTimeOffset Date,
  string? Title,
  string? Comment = null,
  IEnumerable<string>? Tags = null
) : IRequest<Guid>;
