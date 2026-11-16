using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Models;

public record LedgerListItemDTO
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    /// <summary>
    /// The total of the ledger's accounts per currency; zero totals are left out.
    /// </summary>
    public required Balance Balance { get; init; }
}
