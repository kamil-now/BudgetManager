using BudgetManager.Application.Commands;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class DeleteAccountTransactionHandler(IEntityStore store) : IRequestHandler<DeleteAccountTransactionCommand>
{
    public async Task Handle(DeleteAccountTransactionCommand command, CancellationToken cancellationToken)
    {
        var transaction = await store.GetAsync<AccountTransaction>(command.Id, cancellationToken);

        if (transaction.AccountId != command.AccountId)
        {
            throw new NotFoundException($"Transaction {command.Id} not found.");
        }
        if (transaction.IsTransfer)
        {
            throw new ValidationException("Transaction is part of a transfer and can only be deleted with the transfer.");
        }

        // the fund allocations made from it and the payment from a fund are deleted by the database (cascade)
        await store.DeleteAsync<AccountTransaction>(transaction.Id, cancellationToken);

        await store.SaveChangesAsync(cancellationToken);
    }
}
