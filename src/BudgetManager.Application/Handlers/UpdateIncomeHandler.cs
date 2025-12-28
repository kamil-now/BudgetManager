using BudgetManager.Application.Commands;
using BudgetManager.Application.Services;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class UpdateIncomeHandler(ICurrentUserService currentUser, IBudgetManagerService service) : IRequestHandler<UpdateIncomeCommand>
{
    public async Task Handle(UpdateIncomeCommand command, CancellationToken cancellationToken)
    {
        var userId = await currentUser.EnsureAuthenticatedAsync(service, cancellationToken);

        await ValidateCommandAsync(userId, command, cancellationToken);

        var income = await service.GetAsync<Income>(command.Id, cancellationToken);

        income.AccountId = command.AccountId;
        income.Title = command.Title;
        income.Tags = command.Tags?.ToList();
        income.Amount = command.Amount;
        income.Comment = command.Description;
        income.Date = command.Date;

        await service.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateCommandAsync(Guid userId, UpdateIncomeCommand command, CancellationToken cancellationToken)
    {
        await command.AccountId.EnsureAccessibleAsync<Account>(userId, service, cancellationToken);
        await command.Id.EnsureExistsAsync<Income>(service, cancellationToken);

        command.Amount.EnsureValid();
        command.Title?.EnsureNotLongerThan(Constants.MaxTitleLength);
        command.Description?.EnsureNotLongerThan(Constants.MaxCommentLength);
        command.Tags.EnsureValidTags();
    }
}
