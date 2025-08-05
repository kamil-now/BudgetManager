using BudgetManager.Application.Commands;
using BudgetManager.Application.Services;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class CreateLedgerHandler(ICurrentUserService currentUser, IBudgetManagerService budgetService) : IRequestHandler<CreateLedgerCommand, Guid>
{
  public async Task<Guid> Handle(CreateLedgerCommand command, CancellationToken cancellationToken)
  {
    Validate(command);
    var userId = await currentUser.EnsureExistsAsync(budgetService, cancellationToken);

    var budgetId = Guid.NewGuid();
    var entity = await budgetService.CreateAsync(new Ledger
    {
      OwnerId = userId,
      Name = command.Name,
      Description = command.Description,
      Accounts = [.. command.Accounts.Select(x => {
        var accountId = Guid.NewGuid();
        return new Account()
        {
          Id = accountId,
          OwnerId = userId,
          Name = x.Name,
          Description = x.Description,
          Incomes = [new Income() {
            Title = "Initial balance",
            Amount = x.InitialBalance,
            AccountId = accountId
          }]
        };
      })],
      Budgets = [new Budget () {
        Id = budgetId,
        OwnerId = userId,
        Name = command.Budget.Name,
        Description = command.Budget.Description,
        Funds = [.. command.Budget.Funds.Select(x => new Fund(){
          BudgetId = budgetId,
          Name = x.Name,
          Description = x.Description,
          AllocationTemplateSequence = x.AllocationTemplateSequence,
          AllocationTemplateType = x.AllocationTemplateType,
          AllocationTemplateValue = x.AllocationTemplateValue
        })]
      }]
    }, cancellationToken) ?? throw new InvalidOperationException("Failed to create ledger.");

    await budgetService.SaveChangesAsync(cancellationToken);

    if (entity.Id == Guid.Empty)
    {
      throw new InvalidOperationException("Ledger ID is empty.");
    }
    return entity.Id;
  }

  private static void Validate(CreateLedgerCommand command)
  {
    command.Description?.EnsureNotLongerThan(Constants.MaxDescriptionLength);
    command.Accounts.EnsureNotEmpty();

    foreach (var account in command.Accounts)
    {
      account.Name.EnsureNotEmpty().EnsureNotLongerThan(Constants.MaxNameLength);
      account.Description?.EnsureNotLongerThan(Constants.MaxDescriptionLength, $"Description of {account.Name}");
      account.InitialBalance.Amount.EnsureNonnegative($"InitialBalance of {account.Name}");
      account.InitialBalance.Currency.EnsureValidCurrency($"InitialBalance of {account.Name}");
    }

    command.Budget.Name.EnsureNotEmpty().EnsureNotLongerThan(Constants.MaxNameLength);
    command.Budget.Description?.EnsureNotLongerThan(Constants.MaxDescriptionLength);
    command.Budget.Funds.EnsureNotEmpty();

    foreach (var fund in command.Budget.Funds)
    {
      fund.Name.EnsureNotEmpty().EnsureNotLongerThan(Constants.MaxNameLength);
      fund.Description?.EnsureNotLongerThan(Constants.MaxDescriptionLength);
      fund.AllocationTemplateSequence.EnsureNonnegative();
    }
  }
}
