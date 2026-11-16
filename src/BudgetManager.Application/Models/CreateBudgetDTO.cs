namespace BudgetManager.Application.Models;

public record CreateBudgetDTO(
  string Name,
  IEnumerable<CreateFundDTO> Funds,
  IEnumerable<CreateAllocationTemplateDTO>? AllocationTemplates = null);
