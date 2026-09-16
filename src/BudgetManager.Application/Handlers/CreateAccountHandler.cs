using BudgetManager.Application.Commands;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class CreateAccountHandler(IEntityStore store) : IRequestHandler<CreateAccountCommand, Guid>
{
    public async Task<Guid> Handle(CreateAccountCommand command, CancellationToken cancellationToken)
    {
        await ValidateCommandAsync(command, cancellationToken);

        var accountId = Guid.NewGuid();
        var entity = await store.CreateAsync(new Account
        {
            Id = accountId,
            LedgerId = command.LedgerId,
            Name = command.Name,
            Description = command.Description,
            Transactions = [new() { Title = "Initial balance", Value = command.InitialBalance, AccountId = accountId }]
        }, cancellationToken) ?? throw new InvalidOperationException("Failed to create account.");

        await store.SaveChangesAsync(cancellationToken);

        if (entity.Id == Guid.Empty)
        {
            throw new InvalidOperationException("Account ID cannot be empty.");
        }
        return entity.Id;
    }

    private async Task ValidateCommandAsync(CreateAccountCommand command, CancellationToken cancellationToken)
    {
        if (await store.ExistsAsync<Account>(x => x.Name == command.Name && x.LedgerId == command.LedgerId, cancellationToken))
        {
            throw new ConflictException($"Account with name {command.Name} already exists in ledger {command.LedgerId}.");
        }
        command.LedgerId.EnsureNotEmpty();
        command.Name.EnsureNotEmpty().EnsureNotLongerThan(Constants.MaxNameLength);
        command.InitialBalance.EnsureValidInitialBalance();
        command.Description?.EnsureNotLongerThan(Constants.MaxCommentLength);
    }
}
