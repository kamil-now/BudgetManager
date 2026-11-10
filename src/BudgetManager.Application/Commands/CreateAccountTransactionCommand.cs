using BudgetManager.Application.Security;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Commands;

public record CreateAccountTransactionCommand(
  Guid AccountId,
  Money Value,
  DateTimeOffset Date,
  string? Title,
  string? Comment = null,
  IEnumerable<string>? Tags = null
) : IRequest<Guid>, IRequiresAccess
{
    IEnumerable<Resource> IRequiresAccess.Resources => [Resource.Of<Account>(AccountId)];
}
