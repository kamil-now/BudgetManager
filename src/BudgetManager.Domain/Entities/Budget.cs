namespace BudgetManager.Domain.Entities;

public class Budget : Entity
{
  public required Guid OwnerId { get; set; }
  public Guid? LedgerId { get; set; }
  public required string Name { get; set; }
  public string? Description { get; set; }

  public User Owner { get; set; } = null!;
  public Ledger Ledger { get; set; } = null!;
  public virtual ICollection<Fund> Funds { get; set; } = [];
}
