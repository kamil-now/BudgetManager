using BudgetManager.Application.Commands;
using BudgetManager.Application.Services;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class CreateAccountTransferHandler(ICurrentUserService currentUser, IBudgetManagerService service) : IRequestHandler<CreateAccountTransferCommand, Guid>
{
    public async Task<Guid> Handle(CreateAccountTransferCommand command, CancellationToken cancellationToken)
    {
        var userId = await currentUser.EnsureAuthenticatedAsync(service, cancellationToken);

        await ValidateCommandAsync(userId, command, cancellationToken);
        return await service.RunInTransactionAsync(async () =>
        {
            var expense = await service.CreateAsync(new AccountTransaction
            {
                AccountId = command.AccountId,
                Title = command.Title,
                Value = command.Value with { Amount = -command.Value.Amount },
                Comment = command.Comment,
                Date = command.Date,
            }, cancellationToken) ?? throw new InvalidOperationException("Failed to create expense.");

            var income = await service.CreateAsync(new AccountTransaction
            {
                AccountId = command.TargetAccountId,
                Title = command.Title,
                Value = command.Value,
                Comment = command.Comment,
                Date = command.Date,
            }, cancellationToken) ?? throw new InvalidOperationException("Failed to create income.");

            await service.SaveChangesAsync(cancellationToken);

            var transfer = await service.CreateAsync(new AccountTransfer()
            {
                IncomeId = income.Id,
                ExpenseId = expense.Id
            });

            await service.SaveChangesAsync(cancellationToken);
            return transfer.Id;

        }, cancellationToken);
    }

    private async Task ValidateCommandAsync(Guid userId, CreateAccountTransferCommand command, CancellationToken cancellationToken)
    {
        await command.AccountId.EnsureAccessibleAsync<Account>(userId, service, cancellationToken);
        await command.TargetAccountId.EnsureAccessibleAsync<Account>(userId, service, cancellationToken);

        command.Value.EnsureValid();
        command.Title?.EnsureNotLongerThan(Constants.MaxTitleLength);
        command.Comment?.EnsureNotLongerThan(Constants.MaxCommentLength);
    }
}
