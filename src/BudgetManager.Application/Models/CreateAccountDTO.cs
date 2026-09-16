using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Models;

public record CreateAccountDTO(Money InitialBalance, string Name, string? Description = null);
