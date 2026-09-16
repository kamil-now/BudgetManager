using BudgetManager.Domain.Entities;

namespace BudgetManager.Application.Interfaces;

public interface ILedgerReader
{
    Task<Ledger?> ReadAsync(Guid id, CancellationToken cancellationToken = default);
}
