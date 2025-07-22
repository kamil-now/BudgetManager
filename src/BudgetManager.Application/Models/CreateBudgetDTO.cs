namespace BudgetManager.Application.Models;

public record CreateBudgetDTO(string Name, IEnumerable<CreateFundDTO> Funds, string? Description = null);
