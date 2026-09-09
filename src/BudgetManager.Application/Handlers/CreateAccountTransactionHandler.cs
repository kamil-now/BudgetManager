using BudgetManager.Application.Commands;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class CreateAccountTransactionHandler(IBudgetManagerService service) : IRequestHandler<CreateAccountTransactionCommand, Guid>
{
    public async Task<Guid> Handle(CreateAccountTransactionCommand command, CancellationToken cancellationToken)
    {
        ValidateCommand(command);

        var entity = await service.CreateAsync(new AccountTransaction
        {
            AccountId = command.AccountId,
            Title = command.Title,
            Tags = command.Tags?.ToList(),
            Value = command.Value,
            Comment = command.Comment,
            Date = command.Date,
        }, cancellationToken) ?? throw new InvalidOperationException("Failed to create AccountOperation.");

        await service.SaveChangesAsync(cancellationToken);

        if (entity.Id == Guid.Empty)
        {
            throw new InvalidOperationException("AccountOperation ID cannot be empty.");
        }
        return entity.Id;
    }

    private static void ValidateCommand(CreateAccountTransactionCommand command)
    {
        command.Value.EnsureValid();
        command.Title?.EnsureNotLongerThan(Constants.MaxTitleLength);
        command.Comment?.EnsureNotLongerThan(Constants.MaxCommentLength);
        command.Tags.EnsureValidTags();
    }
}
