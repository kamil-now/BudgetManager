namespace BudgetManager.Application.Features.BudgetManagement;

using AutoMapper;
using BudgetManager.Domain.Models;
using BudgetManager.Infrastructure;

public class UpdateAllocationCommandHandler(IUserBudgetRepository repo, IMapper map)
  : UpdateMoneyOperationCommandHandler<UpdateAllocationCommand, Allocation, AllocationDto>(repo, map)
{
  protected override void Update(Allocation operation, UpdateAllocationCommand command)
  => operation.Update(
      command.TargetFundId,
      command.Title,
      command.Value,
      command.Date,
      command.Description
    );
  
  protected override AllocationDto CompleteDto(AllocationDto dto, Budget budget)
    => dto with
    {
      Type = MoneyOperationType.Allocation,
      TargetFundName = budget.Funds.First(x => x.Id == dto.TargetFundId).Name,
    };
}
