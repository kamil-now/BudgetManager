using System.Linq.Expressions;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Domain.Entities;

public class Ledger : Entity, IAccessControlled<Ledger>
{
    public required Guid OwnerId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }

    public User Owner { get; set; } = null!;
    public virtual ICollection<Budget> Budgets { get; set; } = [];
    public virtual ICollection<Account> Accounts { get; set; } = [];

    static Expression<Func<Ledger, Guid>> IAccessControlled<Ledger>.OwnerId => x => x.OwnerId;
}
