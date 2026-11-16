using BudgetManager.Application.Security;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Commands;

public record CreateCurrencyExchangeCommand(
  Guid AccountId,
  Guid? TargetAccountId,
  Money Buy,
  Money Sell,
  DateTimeOffset Date,
  string? Title
) : IRequest<Guid>, IRequiresAccess
{
    IEnumerable<Resource> IRequiresAccess.Resources => TargetAccountId is Guid targetAccountId
      ? [Resource.Of<Account>(AccountId), Resource.Of<Account>(targetAccountId)]
      : [Resource.Of<Account>(AccountId)];
}
