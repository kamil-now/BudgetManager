using BudgetManager.Domain.Enums;

namespace BudgetManager.Domain.Models;

public sealed record FundAllocation(
  string Currency,
  int Sequence,
  AllocationType Type,
  decimal? Amount = null,
  decimal? Percent = null);
