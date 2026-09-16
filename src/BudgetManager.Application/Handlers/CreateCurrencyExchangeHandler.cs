using BudgetManager.Application.Commands;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class CreateCurrencyExchangeHandler(IEntityStore store) : IRequestHandler<CreateCurrencyExchangeCommand, Guid>
{
    public async Task<Guid> Handle(CreateCurrencyExchangeCommand command, CancellationToken cancellationToken)
    {
        ValidateCommand(command);

        var transfer = AccountTransfer.Exchange(
            command.AccountId,
            command.TargetAccountId ?? command.AccountId,
            command.Sell,
            command.Buy,
            command.Date,
            command.Title,
            command.Comment);

        await store.CreateAsync(transfer, cancellationToken);
        await store.SaveChangesAsync(cancellationToken);

        return transfer.Id;
    }

    private static void ValidateCommand(CreateCurrencyExchangeCommand command)
    {
        command.Buy.EnsureValid();
        command.Sell.EnsureValid();

        if (command.Buy.Currency == command.Sell.Currency)
        {
            throw new ValidationException("Buy and sell values cannot have the same currency.");
        }

        command.Title?.EnsureNotLongerThan(Constants.MaxTitleLength);
        command.Comment?.EnsureNotLongerThan(Constants.MaxCommentLength);
    }
}
