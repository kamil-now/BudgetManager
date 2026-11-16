using BudgetManager.Domain.Enums;

namespace BudgetManager.Application.Models;

public record CreateAllocationLineDTO(string Fund, int Sequence, AllocationType Type, decimal? Amount = null, decimal? Percent = null);
