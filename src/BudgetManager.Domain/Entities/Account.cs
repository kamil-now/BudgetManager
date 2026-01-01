using BudgetManager.Common.Models;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Domain.Entities;

public class Account : Entity, IAccessControlled
{
    public required Guid OwnerId { get; set; }
    public Guid? LedgerId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }

    public User Owner { get; set; } = null!;
    public Ledger? Ledger { get; set; }
    public virtual ICollection<AccountTransaction> Transactions { get; set; } = [];

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
