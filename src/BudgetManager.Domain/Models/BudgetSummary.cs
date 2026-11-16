namespace BudgetManager.Domain.Models;

public record BudgetSummary(Guid Id, string Name, IEnumerable<BudgetSummary.Fund> Funds)
{
    public record Fund(Guid Id, string Name, Balance Balance);
}
