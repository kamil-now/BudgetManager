using BudgetManager.Domain.Entities;

namespace BudgetManager.Application.Interfaces;

public interface ILedgerIncomesExpensesReader
{
    Task<IEnumerable<AccountTransaction>> ReadAsync(Guid ledgerId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken = default);
}
