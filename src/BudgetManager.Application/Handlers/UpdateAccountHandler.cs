using BudgetManager.Application.Commands;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class UpdateAccountHandler(IEntityStore store) : IRequestHandler<UpdateAccountCommand>
{
    public async Task Handle(UpdateAccountCommand command, CancellationToken cancellationToken)
    {
        command.Id.EnsureNotEmpty();
        command.Name.EnsureNotEmpty("Account name").EnsureNotLongerThan(Constants.MaxNameLength, "Account name");

        var account = await store.GetAsync<Account>(command.Id, cancellationToken);

        if (await store.ExistsAsync<Account>(
            x => x.LedgerId == account.LedgerId && x.Name == command.Name && x.Id != command.Id,
            cancellationToken))
        {
            throw new ConflictException($"Account with name {command.Name} already exists in ledger {account.LedgerId}.");
        }

        account.Name = command.Name;

        await store.SaveChangesAsync(cancellationToken);
    }
}
