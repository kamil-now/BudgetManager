namespace BudgetManager.Domain.Entities;

public class Fund : Entity
{
  public required Guid BudgetId { get; set; }
  public required string Name { get; set; }
  public string? Description { get; set; }

  public Budget Budget { get; set; } = null!;
  public virtual ICollection<Allocation> Allocations { get; set; } = [];
  public virtual ICollection<Deallocation> Deallocations { get; set; } = [];
  public virtual ICollection<Reallocation> Reallocations { get; set; } = [];
  public virtual ICollection<Reallocation> Unallocations { get; set; } = [];
}
