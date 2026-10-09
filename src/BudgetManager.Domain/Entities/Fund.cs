namespace BudgetManager.Domain.Entities;

public class Fund : Entity
{
    public required Guid BudgetId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }

    public Budget Budget { get; set; } = null!;

    public virtual ICollection<FundTransaction> Transactions { get; set; } = [];

    public virtual ICollection<AllocationTemplateLine> AllocationTemplateLines { get; set; } = [];
}
