using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Interfaces;

public interface ILedgerReader
{
    Task<LedgerSummary?> ReadAsync(Guid id, CancellationToken cancellationToken = default);
}
