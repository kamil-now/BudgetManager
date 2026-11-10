namespace BudgetManager.Application.Models;

public record LedgerListItemDTO
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
}
