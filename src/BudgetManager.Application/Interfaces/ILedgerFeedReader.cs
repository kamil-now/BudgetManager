using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Interfaces;

public interface ILedgerFeedReader
{
    /// <summary>
    /// The page of the ledger's feed that <paramref name="filters"/> select, newest first (date, then creation time, then id),
    /// with the total number of matching items. The filters are expected to be validated.
    /// </summary>
    Task<LedgerFeed> ReadAsync(Guid ledgerId, LedgerFeedFilters filters, CancellationToken cancellationToken = default);
}
