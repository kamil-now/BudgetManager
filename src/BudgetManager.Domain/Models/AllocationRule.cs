using BudgetManager.Domain.Enums;

namespace BudgetManager.Domain.Models;

/// <summary>
/// One step of an allocation pipeline: a fixed amount or a percent (<see cref="Value"/>) for a target, which is a fund
/// in a budget's allocation template and a budget in a ledger's allocation template.
/// </summary>
public sealed record AllocationRule(Guid TargetId, int Sequence, AllocationType Type, decimal Value);
