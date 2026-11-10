using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Interfaces;

public interface IBudgetReader
{
    Task<BudgetSummary?> ReadAsync(Guid id, CancellationToken cancellationToken = default);
}
