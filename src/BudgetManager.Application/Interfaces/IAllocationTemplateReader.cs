using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Interfaces;

public interface IAllocationTemplateReader
{
    /// <summary>
    /// The budget's allocation templates ordered by name, each with its lines ordered by sequence.
    /// </summary>
    Task<IEnumerable<AllocationTemplateDetails>> ReadAsync(Guid budgetId, CancellationToken cancellationToken = default);
}
