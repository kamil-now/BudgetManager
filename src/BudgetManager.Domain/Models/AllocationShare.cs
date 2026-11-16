namespace BudgetManager.Domain.Models;

/// <summary>
/// What one step of an allocation pipeline gives its target (<see cref="AllocationRule.TargetId"/>).
/// </summary>
public sealed record AllocationShare(Guid TargetId, decimal Amount);
