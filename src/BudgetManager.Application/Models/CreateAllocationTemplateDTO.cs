namespace BudgetManager.Application.Models;

/// <summary>
/// A template of a new budget: its steps name the budget's funds by <see cref="CreateAllocationLineDTO.Fund"/>.
/// </summary>
public record CreateAllocationTemplateDTO(string Name, string Currency, IEnumerable<CreateAllocationLineDTO> Lines);
