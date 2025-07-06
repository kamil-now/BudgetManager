using BudgetManager.Application.Commands;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class CreateExpenseHandler(IBudgetManagerService budgetService) : IRequestHandler<CreateExpenseCommand, Guid>
{
  public async Task<Guid> Handle(CreateExpenseCommand command, CancellationToken cancellationToken)
  {
    await ValidateCommandAsync(command, cancellationToken);

    var entity = await budgetService.AddAsync(new Expense
    {
      AccountId = command.AccountId,
      Title = command.Title,
      Tags = command.Tags?.ToList(),
      Amount = command.Amount,
      Description = command.Description,
      Date = command.Date,
    }, cancellationToken) ?? throw new InvalidOperationException("Failed to create expense.");

    if (entity.Id == Guid.Empty)
    {
      throw new InvalidOperationException("Expense ID cannot be empty.");
    }
    return entity.Id;
  }

  private async Task ValidateCommandAsync(CreateExpenseCommand command, CancellationToken cancellationToken)
  {
    if (command.AccountId == Guid.Empty)
    {
      throw new ArgumentException("Account ID cannot be empty.", nameof(command));
    }
    if (!await budgetService.ExistsAsync<Account>(x => x.Id == command.AccountId, cancellationToken))
    {
      throw new InvalidOperationException($"Account with ID {command.AccountId} does not exist.");
    }
    if (!command.Amount.IsPositive())
    {
      throw new ArgumentException("Amount must be greater than zero.", nameof(command));
    }
    if (command.Date == default)
    {
      throw new ArgumentException("Date must be specified.", nameof(command));
    }
    if (command.Tags != null && command.Tags.Any(tag => string.IsNullOrWhiteSpace(tag)))
    {
      throw new ArgumentException("Tags cannot contain null or whitespace values.", nameof(command));
    }
  }
}
