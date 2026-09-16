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
        return await store.RunInTransactionAsync(async () =>
        {
            var expense = await store.CreateAsync(new AccountTransaction
            {
                AccountId = command.AccountId,
                Title = command.Title,
                Value = command.Sell,
                Comment = command.Comment,
                Date = command.Date,
            }, cancellationToken) ?? throw new InvalidOperationException("Failed to create expense.");

            var income = await store.CreateAsync(new AccountTransaction
            {
                AccountId = command.TargetAccountId ?? command.AccountId,
                Title = command.Title,
                Value = command.Buy,
                Comment = command.Comment,
                Date = command.Date,
            }, cancellationToken) ?? throw new InvalidOperationException("Failed to create income.");

            await store.SaveChangesAsync(cancellationToken);

            var transfer = await store.CreateAsync(new AccountTransfer()
            {
                IncomeId = income.Id,
                ExpenseId = expense.Id
            });

            await store.SaveChangesAsync(cancellationToken);
            return transfer.Id;

        }, cancellationToken);
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
