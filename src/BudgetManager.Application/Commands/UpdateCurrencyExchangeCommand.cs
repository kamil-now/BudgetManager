using BudgetManager.Application.Security;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Commands;

public record UpdateCurrencyExchangeCommand(
  Guid Id,
  Guid AccountId,
  Guid? TargetAccountId,
  Money Buy,
  Money Sell,
  DateTimeOffset Date,
  string? Title
) : IRequest, IRequiresAccess
{
    IEnumerable<Resource> IRequiresAccess.Resources => TargetAccountId is Guid targetAccountId
          ? [Resource.Of<AccountTransfer>(Id), Resource.Of<Account>(AccountId), Resource.Of<Account>(targetAccountId)]
          : [Resource.Of<AccountTransfer>(Id), Resource.Of<Account>(AccountId)];
}
