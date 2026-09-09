using BudgetManager.Application.Security;
using BudgetManager.Common.Models;
using BudgetManager.Domain.Entities;

namespace BudgetManager.Application.Commands;

public record CreateAccountTransferCommand(
  Guid AccountId,
  Guid TargetAccountId,
  Money Value,
  DateTimeOffset Date,
  string? Title,
  string? Comment = null,
  IEnumerable<string>? Tags = null
) : IRequest<Guid>, IRequiresAccess
{
  IEnumerable<Resource> IRequiresAccess.Resources => [Resource.Of<Account>(AccountId), Resource.Of<Account>(TargetAccountId)];
}
