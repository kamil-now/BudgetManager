using BudgetManager.Application.Commands;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class CreateAccountTransferHandler(IEntityStore store) : IRequestHandler<CreateAccountTransferCommand, Guid>
{
    public async Task<Guid> Handle(CreateAccountTransferCommand command, CancellationToken cancellationToken)
    {
        ValidateCommand(command);

        var transfer = AccountTransfer.Transfer(
            command.AccountId,
            command.TargetAccountId,
            command.Value,
            command.Date,
            command.Title,
            command.Comment);

        await store.CreateAsync(transfer, cancellationToken);
        await store.SaveChangesAsync(cancellationToken);

        return transfer.Id;
    }

    private static void ValidateCommand(CreateAccountTransferCommand command)
    {
        command.Value.EnsureValid();

        if (command.AccountId == command.TargetAccountId)
        {
            throw new ValidationException("Transfer target account must be different from the source account.");
        }

        command.Title?.EnsureNotLongerThan(Constants.MaxTitleLength);
        command.Comment?.EnsureNotLongerThan(Constants.MaxCommentLength);
    }
}
