using BudgetManager.Application.Commands;
using BudgetManager.Application.Services;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class CreateTransferHandler(ICurrentUserService currentUser, IBudgetManagerService service) : IRequestHandler<CreateTransferCommand, Guid>
{
    public async Task<Guid> Handle(CreateTransferCommand command, CancellationToken cancellationToken)
    {
        await ValidateCommandAsync(command, cancellationToken);

        var entity = await service.CreateAsync(new Transfer
        {
            AccountId = command.AccountId,
            TargetAccountId = command.TargetAccountId,
            Title = command.Title,
            Tags = command.Tags?.ToList(),
            Amount = command.Amount,
            Comment = command.Description,
            Date = command.Date,
        }, cancellationToken) ?? throw new InvalidOperationException("Failed to create transfer.");

        await service.SaveChangesAsync(cancellationToken);

        if (entity.Id == Guid.Empty)
        {
            throw new InvalidOperationException("Transfer ID cannot be empty.");
        }
        return entity.Id;
    }

    private async Task ValidateCommandAsync(CreateTransferCommand command, CancellationToken cancellationToken)
    {
        await command.AccountId.EnsureNotEmpty().EnsureAccessibleAsync<Account>(currentUser, service, cancellationToken);
        await command.TargetAccountId.EnsureNotEmpty().EnsureAccessibleAsync<Account>(currentUser, service, cancellationToken);

        command.Amount.EnsureValid();
        command.Title?.EnsureNotLongerThan(Constants.MaxTitleLength);
        command.Description?.EnsureNotLongerThan(Constants.MaxCommentLength);
        command.Tags.EnsureValidTags();
    }
}
