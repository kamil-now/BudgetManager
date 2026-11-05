using BudgetManager.Application.Commands;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class CreateAccountTransactionHandler(IEntityStore store) : IRequestHandler<CreateAccountTransactionCommand, Guid>
{
    public async Task<Guid> Handle(CreateAccountTransactionCommand command, CancellationToken cancellationToken)
    {
        ValidateCommand(command);

        var entity = await store.CreateAsync(
            AccountTransaction.Create(command.AccountId, command.Value, command.Date, command.Title, command.Comment, command.Tags),
            cancellationToken) ?? throw new InvalidOperationException("Failed to create AccountOperation.");

        await store.SaveChangesAsync(cancellationToken);

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
