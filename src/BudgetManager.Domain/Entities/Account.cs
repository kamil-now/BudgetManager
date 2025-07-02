namespace BudgetManager.Domain.Entities;

public class Account : Entity
{
  public required Guid LedgerId { get; set; }
  public required string Name { get; set; }
  public string? Description { get; set; }

  public Ledger Ledger { get; set; } = null!;
  public virtual ICollection<Income> Incomes { get; set; } = [];
  public virtual ICollection<Expense> Expenses { get; set; } = [];
  public virtual ICollection<Transfer> OutgoingTransfers { get; set; } = [];
  public virtual ICollection<Transfer> IncomingTransfers { get; set; } = [];
}
