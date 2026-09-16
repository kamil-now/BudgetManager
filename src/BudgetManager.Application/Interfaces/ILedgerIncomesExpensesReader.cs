using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Interfaces;

public interface ILedgerIncomesExpensesReader
{
    Task<LedgerStatistics?> ReadAsync(Guid ledgerId, LedgerStatisticsFilters filters, CancellationToken cancellationToken = default);
}
