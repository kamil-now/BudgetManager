using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Interfaces;

public interface IAccountTransferReader
{
    Task<AccountTransferDetails?> ReadAsync(Guid accountId, Guid id, CancellationToken cancellationToken = default);
}
