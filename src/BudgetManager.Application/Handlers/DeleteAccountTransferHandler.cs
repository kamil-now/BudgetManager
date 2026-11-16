using BudgetManager.Application.Commands;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class DeleteAccountTransferHandler(IEntityStore store) : IRequestHandler<DeleteAccountTransferCommand>
{
    public async Task Handle(DeleteAccountTransferCommand command, CancellationToken cancellationToken)
    {
        var transfer = await store.GetAsync<AccountTransfer>(command.Id, [x => x.Expense, x => x.Income], cancellationToken);

        if (transfer.Expense.AccountId != command.AccountId && transfer.Income.AccountId != command.AccountId)
        {
            throw new NotFoundException($"Transfer {command.Id} not found.");
        }

        await store.DeleteAsync<AccountTransfer>(transfer.Id, cancellationToken);
        await store.DeleteAsync<AccountTransaction>(transfer.Expense.Id, cancellationToken);
        await store.DeleteAsync<AccountTransaction>(transfer.Income.Id, cancellationToken);

        await store.SaveChangesAsync(cancellationToken);
    }
}
