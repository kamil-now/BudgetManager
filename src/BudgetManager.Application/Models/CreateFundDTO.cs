using BudgetManager.Common.Enums;

namespace BudgetManager.Application.Models;

public record CreateFundDTO(
  string Name,
  int AllocationTemplateSequence,
  decimal AllocationTemplateValue,
  AllocationType AllocationTemplateType,
  string? Description = null);
