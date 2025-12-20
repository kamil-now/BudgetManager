using BudgetManager.Application.Commands;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class CreateIncomeHandler(IBudgetManagerService budgetService) : IRequestHandler<CreateIncomeCommand, Guid>
{
    public async Task<Guid> Handle(CreateIncomeCommand command, CancellationToken cancellationToken)
    {
        await ValidateCommandAsync(command, cancellationToken);

        var entity = await budgetService.CreateAsync(new Income
        {
            AccountId = command.AccountId,
            Title = command.Title,
            Tags = command.Tags?.ToList(),
            Amount = command.Amount,
            Description = command.Description,
            Date = command.Date,
        }, cancellationToken) ?? throw new InvalidOperationException("Failed to create income.");

        if (entity.Id == Guid.Empty)
        {
            throw new InvalidOperationException("Income ID cannot be empty.");
        }
        return entity.Id;
    }

    private async Task ValidateCommandAsync(CreateIncomeCommand command, CancellationToken cancellationToken)
    {
        command.AccountId.EnsureNotEmpty();

        await command.AccountId.EnsureExists<Account>(budgetService, cancellationToken);

        command.Amount.EnsureValid();
        command.Title?.EnsureNotLongerThan(Constants.MaxTitleLength);
        command.Description?.EnsureNotLongerThan(Constants.MaxDescriptionLength);
        command.Tags.EnsureValidTags();
    }
}
