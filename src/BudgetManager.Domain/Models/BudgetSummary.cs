namespace BudgetManager.Domain.Models;

public record BudgetSummary(Guid Id, string Name, string? Description, IEnumerable<BudgetSummary.Fund> Funds)
{
    public record Fund(Guid Id, string Name, string? Description, Balance Balance);
}
