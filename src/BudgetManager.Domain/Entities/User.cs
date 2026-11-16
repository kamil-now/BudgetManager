using System.Linq.Expressions;
using BudgetManager.Domain.Interfaces;
using BudgetManager.Domain.Models;

namespace BudgetManager.Domain.Entities;

public class User : Entity, IAccessControlled<User>
{
    public string? Name { get; set; }
    public required string Email { get; set; }
    public required string HashedPassword { get; set; }
    public UserPreferences Preferences { get; set; } = new();

    public virtual ICollection<Ledger> Ledgers { get; set; } = [];

    public static Expression<Func<User, Guid>> OwnerId => x => x.Id;
}
