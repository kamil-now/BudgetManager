using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Models;

namespace BudgetManager.Domain.Entities;

public abstract class AllocationTemplateLine : Entity
{
    public required Guid AllocationTemplateId { get; set; }
    public required Guid FundId { get; set; }
    public required int Sequence { get; set; }

    public AllocationTemplate AllocationTemplate { get; set; } = null!;
    public Fund Fund { get; set; } = null!;

    public static AllocationTemplateLine Create(Guid allocationTemplateId, Guid fundId, int sequence, AllocationType type, decimal? amount, decimal? percent) => type switch
    {
        AllocationType.Fixed => new FixedAllocationTemplateLine
        {
            AllocationTemplateId = allocationTemplateId,
            FundId = fundId,
            Sequence = sequence,
            Amount = amount ?? throw new ArgumentNullException(nameof(amount), "A fixed allocation line needs an amount.")
        },
        AllocationType.Percent => new PercentAllocationTemplateLine
        {
            AllocationTemplateId = allocationTemplateId,
            FundId = fundId,
            Sequence = sequence,
            Percent = percent ?? throw new ArgumentNullException(nameof(percent), "A percent allocation line needs a percent.")
        },
        _ => throw new NotSupportedException($"Unsupported allocation type '{type}'.")
    };

    public abstract AllocationRule ToRule();
}
