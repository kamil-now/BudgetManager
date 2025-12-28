using BudgetManager.Application.Commands;
using BudgetManager.Application.Services;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class CreateAccountHandler(ICurrentUserService currentUser, IBudgetManagerService service) : IRequestHandler<CreateAccountCommand, Guid>
{
    public async Task<Guid> Handle(CreateAccountCommand command, CancellationToken cancellationToken)
    {
        var userId = await currentUser.EnsureAuthenticatedAsync(service, cancellationToken);

        await ValidateCommandAsync(userId, command, cancellationToken);

        var accountId = Guid.NewGuid();
        var entity = await service.CreateAsync(new Account
        {
            Id = accountId,
            OwnerId = userId,
            LedgerId = command.LedgerId,
            Name = command.Name,
            Description = command.Description,
            Incomes = [new() { Title = "Initial balance", Amount = command.InitialBalance, AccountId = accountId }]
        }, cancellationToken) ?? throw new InvalidOperationException("Failed to create account.");

        await service.SaveChangesAsync(cancellationToken);

        if (entity.Id == Guid.Empty)
        {
            throw new InvalidOperationException("Account ID cannot be empty.");
        }
        return entity.Id;
    }

    private async Task ValidateCommandAsync(Guid userId, CreateAccountCommand command, CancellationToken cancellationToken)
    {
        if (await service.ExistsAsync<Account>(x => x.Name == command.Name && x.OwnerId == userId, cancellationToken))
        {
            throw new ConflictException($"Account with name {command.Name} already exists for user {userId}.");
        }
        if (command.LedgerId is Guid ledgerId)
        {
            await ledgerId.EnsureExistsAsync<Ledger>(service, cancellationToken);
        }
        command.Name.EnsureNotEmpty().EnsureNotLongerThan(Constants.MaxNameLength);
        command.InitialBalance.EnsureValid();
        command.Description?.EnsureNotLongerThan(Constants.MaxCommentLength);
    }
}
