using BudgetManager.Application.Interfaces;
using BudgetManager.Application.Models;
using BudgetManager.Application.Queries;
using BudgetManager.Application.Validators;

namespace BudgetManager.Application.Handlers;

public sealed class GetAccountTransferHandler(IAccountTransferReader transferReader)
    : IRequestHandler<GetAccountTransferQuery, AccountTransactionDTO>
{
    public async Task<AccountTransactionDTO> Handle(GetAccountTransferQuery query, CancellationToken cancellationToken)
    {
        var transfer = await transferReader.ReadAsync(query.AccountId, query.Id, cancellationToken) ?? throw new NotFoundException($"Transfer {query.Id} not found.");

        var isExchange = transfer.ExpenseValue.Currency != transfer.IncomeValue.Currency;

        return new AccountTransactionDTO(
            transfer.Id,
            transfer.ExpenseAccountId,
            transfer.ExpenseAccountName,
            isExchange ? -transfer.ExpenseValue : transfer.IncomeValue,
            isExchange ? transfer.IncomeValue : null,
            transfer.Date,
            transfer.IncomeAccountId,
            transfer.IncomeAccountName,
            transfer.Title,
            []);
    }
}
