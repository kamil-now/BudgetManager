using BudgetManager.Application.Commands;
using BudgetManager.Application.Services;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class CreateExpenseHandler(ICurrentUserService currentUser, IBudgetManagerService service) : IRequestHandler<CreateExpenseCommand, Guid>
{
    public async Task<Guid> Handle(CreateExpenseCommand command, CancellationToken cancellationToken)
    {
        var userId = await currentUser.EnsureAuthenticatedAsync(service, cancellationToken);

        await ValidateCommandAsync(userId, command, cancellationToken);

        var entity = await service.CreateAsync(new Expense
        {
            AccountId = command.AccountId,
            Title = command.Title,
            Tags = command.Tags?.ToList(),
            Amount = command.Amount,
            Comment = command.Description,
            Date = command.Date,
        }, cancellationToken) ?? throw new InvalidOperationException("Failed to create expense.");

        await service.SaveChangesAsync(cancellationToken);

        if (entity.Id == Guid.Empty)
        {
            throw new InvalidOperationException("Expense ID cannot be empty.");
        }
        return entity.Id;
    }

    private async Task ValidateCommandAsync(Guid userId, CreateExpenseCommand command, CancellationToken cancellationToken)
    {
        await command.AccountId.EnsureAccessibleAsync<Account>(userId, service, cancellationToken);

        command.Amount.EnsureValid();
        command.Title?.EnsureNotLongerThan(Constants.MaxTitleLength);
        command.Description?.EnsureNotLongerThan(Constants.MaxCommentLength);
        command.Tags.EnsureValidTags();
    }
}
