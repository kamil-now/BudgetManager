using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;
using BudgetManager.Application.Queries;
using BudgetManager.Application.Models;

namespace BudgetManager.Application.Handlers;

public sealed class GetFundsHandler(IBudgetService budgetService)
{
  public async Task<IEnumerable<FundDTO>> Execute(GetFundsQuery command, CancellationToken cancellationToken)
  {
    var funds = await budgetService.Get<Fund>(x => x.BudgetId == command.BudgetId, cancellationToken);
    var allocations = await budgetService.Get<Allocation>(x => x.BudgetId == command.BudgetId, cancellationToken);
    var deallocations = await budgetService.Get<Deallocation>(x => x.BudgetId == command.BudgetId, cancellationToken);
    var reallocations = await budgetService.Get<Reallocation>(x => x.BudgetId == command.BudgetId, cancellationToken);

    return funds.Select(fund => 
    {
      var fundDTO = new FundDTO(fund);

      foreach (var allocation in allocations.Where(x => x.FundId == fund.Id))
      {
        fundDTO.Add(allocation.Amount);
      }

      foreach (var deallocation in deallocations.Where(x => x.FundId == fund.Id))
      {
        fundDTO.Deduct(deallocation.Amount);
      }

      foreach (var reallocation in reallocations.Where(x => x.TargetFundId == fund.Id))
      {
        if (reallocation.FundId == fund.Id)
        {
          fundDTO.Deduct(reallocation.Amount);
        }
        else
        {
          fundDTO.Add(reallocation.Amount);
        }
      }

      return fundDTO;
    });
  }
}
