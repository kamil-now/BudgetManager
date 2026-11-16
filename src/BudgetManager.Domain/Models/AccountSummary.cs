namespace BudgetManager.Domain.Models;

public record AccountSummary(Guid Id, Guid LedgerId, string Name,Balance Balance);
