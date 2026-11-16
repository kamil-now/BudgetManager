using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Interfaces;

public interface ILedgerStatisticsReader
{
    Task<LedgerStatistics?> ReadAsync(Guid ledgerId, LedgerStatisticsFilters filters, CancellationToken cancellationToken = default);
}
