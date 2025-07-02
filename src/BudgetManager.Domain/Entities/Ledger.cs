namespace BudgetManager.Domain.Entities;

public class Ledger : Entity
{
  public required Guid UserId { get; set; }
  public required string Name { get; set; }
  public string? Description { get; set; }

  public User User { get; set; } = null!;
  public virtual ICollection<Budget> Budgets { get; set; } = [];
  public virtual ICollection<Account> Accounts { get; set; } = [];
}
