using System.Linq.Expressions;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Domain.Entities;

public class Fund : Entity, IAccessControlled<Fund>
{
    public required Guid BudgetId { get; set; }
    public required string Name { get; set; }

    public Budget Budget { get; set; } = null!;

    public virtual ICollection<FundTransaction> Transactions { get; set; } = [];

    public virtual ICollection<AllocationTemplateLine> AllocationTemplateLines { get; set; } = [];

    public static Expression<Func<Fund, Guid>> OwnerId => x => x.Budget.Ledger.OwnerId;
}
