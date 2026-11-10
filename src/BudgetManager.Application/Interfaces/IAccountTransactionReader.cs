using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Interfaces;

public interface IAccountTransactionReader
{
    Task<AccountTransactionDetails?> ReadAsync(Guid accountId, Guid id, CancellationToken cancellationToken = default);
}
