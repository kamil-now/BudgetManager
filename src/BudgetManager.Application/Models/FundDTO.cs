using BudgetManager.Common.Models;
using BudgetManager.Domain.Entities;

namespace BudgetManager.Application.Models;

public class FundDTO(Fund fund)
{
  public Balance Balance { get; init; } = [];
  public string Name => fund.Name;

  public void Add(Money money) => Balance.Add(money);
  public void Deduct(Money money) => Balance.Deduct(money);
}
