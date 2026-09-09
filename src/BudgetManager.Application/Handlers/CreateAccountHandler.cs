using BudgetManager.Application.Commands;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class CreateAccountHandler(IBudgetManagerService service) : IRequestHandler<CreateAccountCommand, Guid>
{
    public async Task<Guid> Handle(CreateAccountCommand command, CancellationToken cancellationToken)
    {
        await ValidateCommandAsync(command, cancellationToken);

        var accountId = Guid.NewGuid();
        var entity = await service.CreateAsync(new Account
        {
            Id = accountId,
            LedgerId = command.LedgerId,
            Name = command.Name,
            Description = command.Description,
            Transactions = [new() { Title = "Initial balance", Value = command.InitialBalance, AccountId = accountId }]
        }, cancellationToken) ?? throw new InvalidOperationException("Failed to create account.");

        await service.SaveChangesAsync(cancellationToken);

        if (entity.Id == Guid.Empty)
        {
            throw new InvalidOperationException("Account ID cannot be empty.");
        }
        return entity.Id;
    }

    private async Task ValidateCommandAsync(CreateAccountCommand command, CancellationToken cancellationToken)
    {
        if (await service.ExistsAsync<Account>(x => x.Name == command.Name && x.LedgerId == command.LedgerId, cancellationToken))
        {
            throw new ConflictException($"Account with name {command.Name} already exists in ledger {command.LedgerId}.");
        }
        command.LedgerId.EnsureNotEmpty();
        command.Name.EnsureNotEmpty().EnsureNotLongerThan(Constants.MaxNameLength);
        command.InitialBalance.EnsureValid();
        command.Description?.EnsureNotLongerThan(Constants.MaxCommentLength);
    }
}
