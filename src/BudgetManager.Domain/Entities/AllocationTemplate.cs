namespace BudgetManager.Domain.Entities;

public class AllocationTemplate : Entity
{
    public required Guid BudgetId { get; set; }
    public required string Currency { get; set; }

    public Budget Budget { get; set; } = null!;

    public virtual ICollection<AllocationTemplateLine> Lines { get; set; } = [];
}
