namespace BudgetManager.Domain.Entities;

public abstract class AllocationTemplateLine : Entity
{
    public required Guid AllocationTemplateId { get; set; }
    public required Guid FundId { get; set; }
    public required int Sequence { get; set; }

    public AllocationTemplate AllocationTemplate { get; set; } = null!;
    public Fund Fund { get; set; } = null!;
}
