using System.Linq.Expressions;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Domain.Entities;

public class Budget : Entity, IAccessControlled<Budget>
{
    public required Guid LedgerId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }

    public Ledger Ledger { get; set; } = null!;
    public virtual ICollection<Fund> Funds { get; set; } = [];

    public static Expression<Func<Budget, Guid>> OwnerId => x => x.Ledger.OwnerId;
}
