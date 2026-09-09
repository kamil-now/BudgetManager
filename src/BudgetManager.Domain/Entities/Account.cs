using System.Linq.Expressions;
using BudgetManager.Common.Models;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Domain.Entities;

public class Account : Entity, IAccessControlled<Account>
{
    public required Guid LedgerId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }

    public Ledger Ledger { get; set; } = null!;
    public virtual ICollection<AccountTransaction> Transactions { get; set; } = [];

    public static Expression<Func<Account, Guid>> OwnerId => x => x.Ledger.OwnerId;

    public Balance GetBalance()
    {
        Balance balance = [];
        foreach (var x in Transactions)
        {
            balance.Add(x.Value);
        }
        foreach (var key in balance.Where(x => x.Value == 0).Select(x => x.Key).ToList())
        {
            balance.Remove(key);
        }
        return balance;
    }
}
