using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Interfaces;

public interface ILedgerAllocationTemplateReader
{
    /// <summary>
    /// The ledger's allocation templates ordered by name, each with its lines ordered by sequence.
    /// </summary>
    Task<IEnumerable<LedgerAllocationTemplateDetails>> ReadAsync(Guid ledgerId, CancellationToken cancellationToken = default);
}
