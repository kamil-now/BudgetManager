using BudgetManager.Application.Commands;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class UpdateAccountTransferHandler(IEntityStore store) : IRequestHandler<UpdateAccountTransferCommand>
{
    public async Task Handle(UpdateAccountTransferCommand command, CancellationToken cancellationToken)
    {
        ValidateCommand(command);

        var transfer = await store.GetAsync<AccountTransfer>(command.Id, [x => x.Expense, x => x.Income], cancellationToken);

        transfer.UpdateTransfer(
            command.AccountId,
            command.TargetAccountId,
            command.Value,
            command.Date,
            command.Title,
            command.Comment,
            command.Tags);

        await store.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateCommand(UpdateAccountTransferCommand command)
    {
        command.Value.EnsureValidPositive();

        if (command.AccountId == command.TargetAccountId)
        {
            throw new ValidationException("Transfer target account must be different from the source account.");
        }

        command.Title?.EnsureNotLongerThan(Constants.MaxTitleLength);
        command.Comment?.EnsureNotLongerThan(Constants.MaxCommentLength);
        command.Tags.EnsureValidTags();
    }
}
