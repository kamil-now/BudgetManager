using BudgetManager.Application.Commands;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class CreateAccountHandler(IBudgetManagerService budgetService) : IRequestHandler<CreateAccountCommand, Guid>
{
  public async Task<Guid> Handle(CreateAccountCommand command, CancellationToken cancellationToken)
  {
    if (await budgetService.Exists<Account>(a => a.Name == command.Name && a.UserId == command.UserId, cancellationToken))
    {
      throw new InvalidOperationException($"Account with name {command.Name} already exists for user {command.UserId}.");
    }
  if (command.LedgerId.HasValue && !await budgetService.Exists<Ledger>(l => l.Id == command.LedgerId.Value, cancellationToken))
    {
      throw new InvalidOperationException($"Ledger with ID {command.LedgerId.Value} does not exist.");
    }
    var entity = await budgetService.Add(new Account
    {
      UserId = command.UserId,
      LedgerId = command.LedgerId,
      Name = command.Name,
      Description = command.Description,
    }, cancellationToken) ?? throw new InvalidOperationException("Failed to create account.");
    if (entity.Id == Guid.Empty)
    {
      throw new InvalidOperationException("Account ID cannot be empty.");
    }
    return entity.Id;
  }
}
