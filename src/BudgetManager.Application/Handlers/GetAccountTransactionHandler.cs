using BudgetManager.Application.Interfaces;
using BudgetManager.Application.Models;
using BudgetManager.Application.Queries;
using BudgetManager.Application.Validators;

namespace BudgetManager.Application.Handlers;

public sealed class GetAccountTransactionHandler(IAccountTransactionReader transactionReader)
    : IRequestHandler<GetAccountTransactionQuery, AccountTransactionDTO>
{
    public async Task<AccountTransactionDTO> Handle(GetAccountTransactionQuery query, CancellationToken cancellationToken)
    {
        var transaction = await transactionReader.ReadAsync(query.AccountId, query.Id, cancellationToken);
        if (transaction is null)
        {
            throw new NotFoundException($"Transaction {query.Id} not found.");
        }

        return new AccountTransactionDTO(
            transaction.Id,
            transaction.AccountId,
            transaction.AccountName,
            transaction.Value,
            null,
            transaction.Date,
            null,
            null,
            transaction.Title,
            transaction.Comment,
            transaction.Tags);
    }
}
