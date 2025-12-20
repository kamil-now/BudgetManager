using BudgetManager.Application.Commands;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class CreateTransferHandler(IBudgetManagerService budgetService) : IRequestHandler<CreateTransferCommand, Guid>
{
    public async Task<Guid> Handle(CreateTransferCommand command, CancellationToken cancellationToken)
    {
        await ValidateCommandAsync(command, cancellationToken);

        var entity = await budgetService.CreateAsync(new Transfer
        {
            AccountId = command.AccountId,
            TargetAccountId = command.TargetAccountId,
            Title = command.Title,
            Tags = command.Tags?.ToList(),
            Amount = command.Amount,
            Description = command.Description,
            Date = command.Date,
        }, cancellationToken) ?? throw new InvalidOperationException("Failed to create transfer.");

        if (entity.Id == Guid.Empty)
        {
            throw new InvalidOperationException("Transfer ID cannot be empty.");
        }
        return entity.Id;
    }

    private async Task ValidateCommandAsync(CreateTransferCommand command, CancellationToken cancellationToken)
    {
        command.AccountId.EnsureNotEmpty();
        command.TargetAccountId.EnsureNotEmpty();
        
        await command.AccountId.EnsureExists<Account>(budgetService, cancellationToken);
        await command.TargetAccountId.EnsureExists<Account>(budgetService, cancellationToken);

        command.Amount.EnsureValid();
        command.Title?.EnsureNotLongerThan(Constants.MaxTitleLength);
        command.Description?.EnsureNotLongerThan(Constants.MaxDescriptionLength);
        command.Tags.EnsureValidTags();
    }
}
