using BudgetManager.Domain.Models;
using BudgetManager.Domain.Entities;

namespace BudgetManager.Application.Models;

public class FundDTO(Fund fund)
{
    public Balance Balance { get; init; } = fund.GetBalance();
    public string Name => fund.Name;
}
