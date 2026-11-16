using BudgetManager.Application.Models;
using BudgetManager.Application.Security;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Commands;

/// <summary>
/// Creates an income or expense. An income can be allocated to funds with <see cref="Allocation"/>;
/// an expense can be paid from one fund with <see cref="FundId"/>, which then lowers that fund instead of the unallocated money.
/// </summary>
public record CreateAccountTransactionCommand(
  Guid AccountId,
  Money Value,
  DateTimeOffset Date,
  string? Title,
  AllocationInput? Allocation = null,
  Guid? FundId = null
) : IRequest<Guid>, IRequiresAccess
{
    IEnumerable<Resource> IRequiresAccess.Resources =>
      [
        Resource.Of<Account>(AccountId),
        .. Allocation?.ToResources() ?? Enumerable.Empty<Resource>(),
        .. FundId is { } fundId ? [Resource.Of<Fund>(fundId)] : Enumerable.Empty<Resource>()
      ];
}
