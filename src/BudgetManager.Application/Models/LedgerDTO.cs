using BudgetManager.Common.Models;

namespace BudgetManager.Application.Models;

public record LedgerDTO
{
    public required Balance Balance { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required IEnumerable<Budget> Budgets { get; init; }
    public required IEnumerable<Account> Accounts { get; init; }

    public record Account
    {
        public required Balance Balance { get; init; }
        public required string Name { get; init; }
        public string? Description { get; init; }
    };

    public class Budget
    {
        public required Balance Balance { get; init; }
        public required string Name { get; init; }
        public string? Description { get; init; }
        public required IEnumerable<Fund> Funds { get; init; }
    }

    public record Fund
    {
        public required Balance Balance { get; init; }
        public required string Name { get; init; }
        public string? Description { get; init; }
    }
}
