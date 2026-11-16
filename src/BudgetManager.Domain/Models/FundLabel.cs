namespace BudgetManager.Domain.Models;

public record FundLabel(Guid FundId, string FundName, Guid BudgetId, string BudgetName);
