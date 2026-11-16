using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Models;

public record AccountDTO
{
    public required Guid Id { get; init; }
    public required Guid LedgerId { get; init; }
    public required Balance Balance { get; init; }
    public required string Name { get; init; }
}
