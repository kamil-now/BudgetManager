using BudgetManager.Common.Models;

namespace BudgetManager.Application.Commands;

public record CreateAccountTransferCommand(
  Guid AccountId,
  Guid TargetAccountId,
  Money Value,
  DateTimeOffset Date,
  string? Title,
  string? Comment = null,
  IEnumerable<string>? Tags = null
) : IRequest<Guid>;
