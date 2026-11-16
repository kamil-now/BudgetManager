using BudgetManager.Domain.Enums;

namespace BudgetManager.Domain.Models;

public sealed record AllocationLine(
  Guid FundId,
  int Sequence,
  AllocationType Type,
  decimal? Amount = null,
  decimal? Percent = null);
