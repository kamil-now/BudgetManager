using BudgetManager.Common.Models;

namespace BudgetManager.Domain.Entities;

public class Account : Entity
{
    public required Guid OwnerId { get; set; }
    public Guid? LedgerId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }

    public User Owner { get; set; } = null!;
    public Ledger? Ledger { get; set; }
    public virtual ICollection<Income> Incomes { get; set; } = [];
    public virtual ICollection<Expense> Expenses { get; set; } = [];
    public virtual ICollection<Transfer> IncomingTransfers { get; set; } = [];
    public virtual ICollection<Transfer> OutgoingTransfers { get; set; } = [];
    

    public Balance GetBalance()
    {
        Balance balance = [];
        foreach (var x in Incomes)
        {
            balance.Add(x.Amount);
        }
        foreach (var x in Expenses)
        {
            balance.Deduct(x.Amount);
        }
        foreach (var x in IncomingTransfers)
        {
            balance.Add(x.Amount);
        }
        foreach (var x in OutgoingTransfers)
        {
            balance.Deduct(x.Amount);
        }
        return balance;
    }
}
