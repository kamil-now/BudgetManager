using BudgetManager.Application.Commands;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class UpdateAccountTransactioHandler(IBudgetManagerService service) : IRequestHandler<UpdateAccountTransactionCommand>
{
    public async Task Handle(UpdateAccountTransactionCommand command, CancellationToken cancellationToken)
    {
        ValidateCommand(command);

        var income = await service.GetAsync<AccountTransaction>(command.Id, cancellationToken);

        if (income.AccountId != command.AccountId)
        {
            var currentAccount = await service.GetAsync<Account>(income.AccountId, cancellationToken);
            var targetAccount = await service.GetAsync<Account>(command.AccountId, cancellationToken);

            if (currentAccount.LedgerId != targetAccount.LedgerId)
            {
                throw new ValidationException("Transaction target account must belong to the same ledger as the current account.");
            }
        }

        income.AccountId = command.AccountId;
        income.Title = command.Title;
        income.Tags = command.Tags?.ToList();
        income.Value = command.Amount;
        income.Comment = command.Comment;
        income.Date = command.Date;

        await service.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateCommand(UpdateAccountTransactionCommand command)
    {
        command.Amount.EnsureValid();
        command.Title?.EnsureNotLongerThan(Constants.MaxTitleLength);
        command.Comment?.EnsureNotLongerThan(Constants.MaxCommentLength);
        command.Tags.EnsureValidTags();
    }
}
