using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Interfaces;

public interface IAccountReader
{
    Task<AccountSummary?> ReadAsync(Guid id, CancellationToken cancellationToken = default);
}
