using BudgetManager.Common.Models;

namespace BudgetManager.Application.Models;

public record LedgerDTO
{
    public required Guid Id { get; init; }
    public required Balance Balance { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required IEnumerable<Budget> Budgets { get; init; }
    public required IEnumerable<Account> Accounts { get; init; }

    public record Account
    {
        public required Guid Id { get; init; }
        public required Balance Balance { get; init; }
        public required string Name { get; init; }
        public string? Description { get; init; }
    };

    public record Budget
    {
        public required Guid Id { get; init; }
        public required Balance Balance { get; init; }
        public required string Name { get; init; }
        public string? Description { get; init; }
        public required IEnumerable<Fund> Funds { get; init; }
    }

    public record Fund
    {
        public Guid Id { get; init; }
        public required Balance Balance { get; init; }
        public required string Name { get; init; }
        public string? Description { get; init; }
    }
}
