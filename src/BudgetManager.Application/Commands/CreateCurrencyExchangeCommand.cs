using BudgetManager.Application.Security;
using BudgetManager.Domain.Models;
using BudgetManager.Domain.Entities;

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
) : IRequest<Guid>, IRequiresAccess
{
  IEnumerable<Resource> IRequiresAccess.Resources => TargetAccountId is Guid targetAccountId
    ? [Resource.Of<Account>(AccountId), Resource.Of<Account>(targetAccountId)]
    : [Resource.Of<Account>(AccountId)];
}
