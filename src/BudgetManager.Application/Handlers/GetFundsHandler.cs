using BudgetManager.Application.Models;
using BudgetManager.Application.Queries;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class GetFundsHandler(IBudgetService budgetService) : IRequestHandler<GetFundsQuery, IEnumerable<FundDTO>>
{
  public async Task<IEnumerable<FundDTO>> Handle(GetFundsQuery command, CancellationToken cancellationToken)
  {
    var funds = await budgetService.GetAllFundsWithTransactions(command.BudgetId, cancellationToken);

    return funds.Select(fund => new FundDTO(fund));
  }
}
