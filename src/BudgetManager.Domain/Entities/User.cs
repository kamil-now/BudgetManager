namespace BudgetManager.Domain.Entities;

public class User : Entity
{
  public required string Name { get; set; }
  public required string Email { get; set; }
  
  public virtual ICollection<Budget> Budgets { get; set; } = [];
  public virtual ICollection<Ledger> Ledgers { get; set; } = [];
}
