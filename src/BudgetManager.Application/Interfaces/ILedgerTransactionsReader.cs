using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Interfaces;

public interface ILedgerTransactionsReader
{
    Task<LedgerTransactions> ReadAsync(Guid ledgerId, LedgerTransactionsFilters filters, CancellationToken cancellationToken = default);
}
