using BudgetManager.Common.Models;

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

  public Balance GetBalance()
  {
    Balance balance = [];
    foreach (var x in Allocations)
    {
      balance.Add(x.Amount);
    }
    foreach (var x in Deallocations)
    {
      balance.Deduct(x.Amount);
    }
    foreach (var x in Reallocations)
    {
      if (x.FundId == Id)
      {
        balance.Deduct(x.Amount);
      }
      else
      {
        balance.Add(x.Amount);
      }
    }
    return balance;
  }
}
