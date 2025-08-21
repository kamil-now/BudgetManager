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
    public virtual ICollection<Transfer> OutgoingTransfers { get; set; } = [];
    public virtual ICollection<Transfer> IncomingTransfers { get; set; } = [];
}
