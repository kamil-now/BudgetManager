using BudgetManager.Application.Commands;
using BudgetManager.Application.Services;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class UpdateAccountTransactioHandler(ICurrentUserService currentUser, IBudgetManagerService service) : IRequestHandler<UpdateAccountTransactionCommand>
{
    public async Task Handle(UpdateAccountTransactionCommand command, CancellationToken cancellationToken)
    {
        var userId = await currentUser.EnsureAuthenticatedAsync(service, cancellationToken);

        await ValidateCommandAsync(userId, command, cancellationToken);

        var income = await service.GetAsync<AccountTransaction>(command.Id, cancellationToken);

        income.AccountId = command.AccountId;
        income.Title = command.Title;
        income.Tags = command.Tags?.ToList();
        income.Value = command.Amount;
        income.Comment = command.Comment;
        income.Date = command.Date;

        await service.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateCommandAsync(Guid userId, UpdateAccountTransactionCommand command, CancellationToken cancellationToken)
    {
        await command.AccountId.EnsureAccessibleAsync<Account>(userId, service, cancellationToken);
        await command.Id.EnsureExistsAsync<AccountTransaction>(service, cancellationToken);

        command.Amount.EnsureValid();
        command.Title?.EnsureNotLongerThan(Constants.MaxTitleLength);
        command.Comment?.EnsureNotLongerThan(Constants.MaxCommentLength);
        command.Tags.EnsureValidTags();
    }
}
