using BudgetManager.Application.Security;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Commands;

public record UpdateAccountTransactionCommand(
  Guid Id,
  Guid AccountId,
  Money Amount,
  DateTimeOffset Date,
  string? Title,
  string? Comment = null,
  IEnumerable<string>? Tags = null
) : IRequest, IRequiresAccess
{
    IEnumerable<Resource> IRequiresAccess.Resources =>
      [Resource.Of<Account>(AccountId), Resource.Of<AccountTransaction>(Id)];
}
