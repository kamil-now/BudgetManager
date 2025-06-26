using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;
using BudgetManager.Application.Commands;

namespace BudgetManager.Application.Handlers;

public sealed class CreateExpenseHandler(IBudgetService budgetService)
{
  public async Task<Expense> Execute(CreateExpenseCommand command, CancellationToken cancellationToken)
  {
    return await budgetService.Add(new Expense
    {
      AccountId = command.AccountId,
      Title = command.Title,
      Tags = command.Tags?.ToList(),
      LedgerId = command.LedgerId,
      Amount = command.Amount,
      Description = command.Description,
      Date = command.Date,
    }, cancellationToken);
  }
}
