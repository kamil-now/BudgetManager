using BudgetManager.Application.Security;
using BudgetManager.Domain.Models;
using BudgetManager.Domain.Entities;

namespace BudgetManager.Application.Commands;

public record UpdateCurrencyExchangeCommand(
  Guid Id,
  Guid AccountId,
  Guid? TargetAccountId,
  Money Buy,
  Money Sell,
  DateTimeOffset Date,
  string? Title,
  string? Comment = null,
  IEnumerable<string>? Tags = null
) : IRequest, IRequiresAccess
{
  IEnumerable<Resource> IRequiresAccess.Resources => TargetAccountId is Guid targetAccountId
    ? [Resource.Of<AccountTransfer>(Id), Resource.Of<Account>(AccountId), Resource.Of<Account>(targetAccountId)]
    : [Resource.Of<AccountTransfer>(Id), Resource.Of<Account>(AccountId)];
}
