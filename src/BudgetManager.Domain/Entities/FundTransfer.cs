namespace BudgetManager.Domain.Entities;

public sealed class FundTransfer : Entity
{
    public required Guid AllocationId { get; set; }
    public required Guid DeallocationId { get; set; }

    public FundTransaction Allocation { get; set; } = null!;
    public FundTransaction Deallocation { get; set; } = null!;
}
