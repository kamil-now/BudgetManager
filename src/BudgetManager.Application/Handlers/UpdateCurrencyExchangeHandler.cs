using BudgetManager.Application.Commands;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class UpdateCurrencyExchangeHandler(IEntityStore store) : IRequestHandler<UpdateCurrencyExchangeCommand>
{
    public async Task Handle(UpdateCurrencyExchangeCommand command, CancellationToken cancellationToken)
    {
        ValidateCommand(command);

        var transfer = await store.GetAsync<AccountTransfer>(command.Id, [x => x.Expense, x => x.Income], cancellationToken);

        transfer.UpdateExchange(
            command.AccountId,
            command.TargetAccountId ?? command.AccountId,
            command.Sell,
            command.Buy,
            command.Date,
            command.Title,
            command.Comment,
            command.Tags);

        await store.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateCommand(UpdateCurrencyExchangeCommand command)
    {
        command.Buy.EnsureValidPositive();
        command.Sell.EnsureValidPositive();

        if (command.Buy.Currency == command.Sell.Currency)
        {
            throw new ValidationException("Buy and sell values cannot have the same currency.");
        }

        command.Title?.EnsureNotLongerThan(Constants.MaxTitleLength);
        command.Comment?.EnsureNotLongerThan(Constants.MaxCommentLength);
        command.Tags.EnsureValidTags();
    }
}
