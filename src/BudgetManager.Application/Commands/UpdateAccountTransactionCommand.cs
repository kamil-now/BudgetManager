using BudgetManager.Application.Models;
using BudgetManager.Application.Security;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Commands;

/// <summary>
/// Updates an income or expense. <see cref="Allocation"/> replaces the fund allocations made from it; when it is left out
/// the existing allocations are kept and follow the transaction's date and title.
/// <see cref="FundId"/> is the fund an expense is paid from; it replaces the existing payment, so null means no fund.
/// </summary>
public record UpdateAccountTransactionCommand(
  Guid Id,
  Guid AccountId,
  Money Amount,
  DateTimeOffset Date,
  string? Title,
  AllocationInput? Allocation = null,
  Guid? FundId = null
) : IRequest, IRequiresAccess
{
    IEnumerable<Resource> IRequiresAccess.Resources =>
      [
        Resource.Of<Account>(AccountId),
        Resource.Of<AccountTransaction>(Id),
        .. Allocation?.ToResources() ?? Enumerable.Empty<Resource>(),
        .. FundId is { } fundId ? [Resource.Of<Fund>(fundId)] : Enumerable.Empty<Resource>()
      ];
}
