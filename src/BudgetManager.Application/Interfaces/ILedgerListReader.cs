using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Interfaces;

public interface ILedgerListReader
{
    Task<IReadOnlyList<LedgerListItem>> ReadAllAsync(Guid ownerId, CancellationToken cancellationToken = default);
}
