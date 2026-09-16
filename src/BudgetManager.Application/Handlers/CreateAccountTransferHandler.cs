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
        return await store.RunInTransactionAsync(async () =>
        {
            var expense = await store.CreateAsync(new AccountTransaction
            {
                AccountId = command.AccountId,
                Title = command.Title,
                Value = command.Value with { Amount = -command.Value.Amount },
                Comment = command.Comment,
                Date = command.Date,
            }, cancellationToken) ?? throw new InvalidOperationException("Failed to create expense.");

            var income = await store.CreateAsync(new AccountTransaction
            {
                AccountId = command.TargetAccountId,
                Title = command.Title,
                Value = command.Value,
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
