using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Models;

public record BudgetDTO
{
    public required Guid Id { get; init; }
    public required Balance Balance { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required IEnumerable<Fund> Funds { get; init; }

    public record Fund
    {
        public required Guid Id { get; init; }
        public required Balance Balance { get; init; }
        public required string Name { get; init; }
        public string? Description { get; init; }
    }
}
