using BudgetManager.Application.Security;
using BudgetManager.Domain.Models;
using BudgetManager.Domain.Entities;

namespace BudgetManager.Application.Commands;

public record UpdateAccountTransferCommand(
  Guid Id,
  Guid AccountId,
  Guid TargetAccountId,
  Money Value,
  DateTimeOffset Date,
  string? Title,
  string? Comment = null,
  IEnumerable<string>? Tags = null
) : IRequest, IRequiresAccess
{
  IEnumerable<Resource> IRequiresAccess.Resources =>
    [Resource.Of<AccountTransfer>(Id), Resource.Of<Account>(AccountId), Resource.Of<Account>(TargetAccountId)];
}
