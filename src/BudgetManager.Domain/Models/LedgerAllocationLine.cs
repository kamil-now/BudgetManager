using BudgetManager.Domain.Enums;

namespace BudgetManager.Domain.Models;

public sealed record LedgerAllocationLine(
  Guid BudgetId,
  int Sequence,
  AllocationType Type,
  decimal? Amount = null,
  decimal? Percent = null);
