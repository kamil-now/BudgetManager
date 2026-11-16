using BudgetManager.Application.Security;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Commands;

public record UpdateAccountTransferCommand(
  Guid Id,
  Guid AccountId,
  Guid TargetAccountId,
  Money Value,
  DateTimeOffset Date,
  string? Title
) : IRequest, IRequiresAccess
{
    IEnumerable<Resource> IRequiresAccess.Resources =>
      [Resource.Of<AccountTransfer>(Id), Resource.Of<Account>(AccountId), Resource.Of<Account>(TargetAccountId)];
}
