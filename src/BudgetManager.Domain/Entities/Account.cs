using BudgetManager.Common.Models;

namespace BudgetManager.Domain.Entities;

public class Account : Entity
{
  public required Balance Balance { get; init; }
  public required string Name { get; init; }

  public void Add(Money money) => Balance.Add(money);
  public void Deduct(Money money) => Balance.Deduct(money);
}
