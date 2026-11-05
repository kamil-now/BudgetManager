using BudgetManager.Application.Commands;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class UpdateAccountTransactioHandler(IEntityStore store) : IRequestHandler<UpdateAccountTransactionCommand>
{
    public async Task Handle(UpdateAccountTransactionCommand command, CancellationToken cancellationToken)
    {
        ValidateCommand(command);

        var transaction = await store.GetAsync<AccountTransaction>(command.Id, cancellationToken);

        if (transaction.IsTransfer)
        {
            throw new ValidationException("Transaction is part of a transfer and can only be changed through the transfer.");
        }

        if (transaction.AccountId != command.AccountId)
        {
            if (transaction.Type == AccountTransactionType.InitialBalance)
            {
                throw new ValidationException("Initial balance cannot be moved to another account.");
            }

            var currentAccount = await store.GetAsync<Account>(transaction.AccountId, cancellationToken);
            var targetAccount = await store.GetAsync<Account>(command.AccountId, cancellationToken);

            if (currentAccount.LedgerId != targetAccount.LedgerId)
            {
                throw new ValidationException("Transaction target account must belong to the same ledger as the current account.");
            }
        }

        transaction.Update(command.AccountId, command.Amount, command.Date, command.Title, command.Comment, command.Tags);

        await store.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateCommand(UpdateAccountTransactionCommand command)
    {
        command.Amount.EnsureValid();
        command.Title?.EnsureNotLongerThan(Constants.MaxTitleLength);
        command.Comment?.EnsureNotLongerThan(Constants.MaxCommentLength);
        command.Tags.EnsureValidTags();
    }
}
