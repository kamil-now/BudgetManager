namespace BudgetManager.Domain.Models;

public class Fund
{
  public string? Id { get; }
  public string Name { get; internal set; }
  public Balance Balance { get; }
  public bool IsDefault { get; internal set; }

  public Fund(string id, string name, bool isDefault = false)
  {
    Id = id;
    Name = name;
    Balance = new Balance();
    IsDefault = isDefault;
  }
  public void Add(Money money) => Balance.Add(money);
  public void Deduct(Money money) => Balance.Deduct(money);
}
