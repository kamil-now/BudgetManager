using BudgetManager.Application.Models;
using BudgetManager.Application.Queries;
using BudgetManager.Application.Services;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class GetLedgerTransactionsHandler(ICurrentUserService currentUser, IBudgetManagerService service) : IRequestHandler<GetLedgerTransactionsQuery, LedgerTransactionsDTO?>
{
    public async Task<LedgerTransactionsDTO?> Handle(GetLedgerTransactionsQuery query, CancellationToken cancellationToken)
    {
        var userId = await currentUser.EnsureAuthenticatedAsync(service, cancellationToken);

        await query.LedgerId.EnsureAccessibleAsync<Ledger>(userId, service, cancellationToken);

        var filters = query.Filters;
        filters.From ??= DateTimeOffset.MinValue;
        filters.To ??= DateTimeOffset.MaxValue;

        var ledgerTransactions = await service.GetLedgerTransactionsAsync(query.LedgerId, filters, cancellationToken);

        var accountTransfers = ledgerTransactions.AccountTransfers
                .Select(transfer => new
                {
                    Transfer = transfer,
                    Income = ledgerTransactions.AccountTransactions.First(x => x.Id == transfer.IncomeId),
                    Expense = ledgerTransactions.AccountTransactions.First(x => x.Id == transfer.ExpenseId)
                }).Where(x => x.Income.Value.Currency == x.Expense.Value.Currency && x.Income.AccountId != x.Expense.AccountId)
                .ToArray();

        var currencyExchanges = ledgerTransactions.AccountTransfers
                .Select(transfer => new
                {
                    Transfer = transfer,
                    Income = ledgerTransactions.AccountTransactions.First(x => x.Id == transfer.IncomeId),
                    Expense = ledgerTransactions.AccountTransactions.First(x => x.Id == transfer.ExpenseId)
                }).Where(x => x.Income.Value.Currency != x.Expense.Value.Currency)
                .ToArray();

        var reallocations = ledgerTransactions.FundTransfers
                .Select(transfer => new
                {
                    Transfer = transfer,
                    Allocation = ledgerTransactions.FundTransactions.First(x => x.Id == transfer.AllocationId),
                    Deallocation = ledgerTransactions.FundTransactions.First(x => x.Id == transfer.DeallocationId)
                }).Where(x => x.Allocation.Value.Currency == x.Deallocation.Value.Currency && x.Allocation.FundId != x.Deallocation.FundId)
                .ToArray();

        return new LedgerTransactionsDTO()
        {
            Incomes = [.. ledgerTransactions.AccountTransactions
                .Where(x => x.Value.Amount > 0 && x.InTransfer == null && x.OutTransfer == null)
                .Select(x => new LedgerTransactionsDTO.AccountTransaction(
                    x.Id,
                    x.AccountId,
                    ledgerTransactions.Accounts[x.AccountId],
                    x.Value,
                    null,
                    x.Date,
                    null,
                    null,
                    x.Title,
                    x.Comment,
                    x.Tags
            ))],

            Expenses = [.. ledgerTransactions.AccountTransactions
                .Where(x => x.Value.Amount < 0 && x.InTransfer == null && x.OutTransfer == null)
                .Select(x => new LedgerTransactionsDTO.AccountTransaction(
                    x.Id,
                    x.AccountId,
                    ledgerTransactions.Accounts[x.AccountId],
                    x.Value,
                    null,
                    x.Date,
                    null,
                    null,
                    x.Title,
                    x.Comment,
                    x.Tags
            ))],

            Transfers = [.. accountTransfers
                .Select(x => new LedgerTransactionsDTO.AccountTransaction(
                    x.Transfer.Id,
                    x.Expense.AccountId,
                    ledgerTransactions.Accounts[x.Expense.AccountId],
                    x.Income.Value,
                    null,
                    x.Income.Date,
                    x.Income.AccountId,
                    ledgerTransactions.Accounts[x.Income.AccountId],
                    x.Income.Title,
                    x.Income.Comment,
                    x.Income.Tags
            ))],

            CurrencyExchanges = [.. currencyExchanges
                .Select(x => new LedgerTransactionsDTO.AccountTransaction(
                    x.Transfer.Id,
                    x.Expense.AccountId,
                    ledgerTransactions.Accounts[x.Expense.AccountId],
                    x.Expense.Value,
                    x.Income.Value,
                    x.Income.Date,
                    x.Income.AccountId,
                    ledgerTransactions.Accounts[x.Income.AccountId],
                    x.Income.Title,
                    x.Income.Comment,
                    x.Income.Tags
            ))],

            Allocations = [.. ledgerTransactions.FundTransactions
                .Where(x => x.Value.Amount > 0 && x.InTransfer == null && x.OutTransfer == null)
                .Select(x => new LedgerTransactionsDTO.FundTransaction(
                    x.Id,
                    ledgerTransactions.Funds[x.FundId].Item1,
                    ledgerTransactions.Budgets[ledgerTransactions.Funds[x.FundId].Item1],
                    x.FundId,
                    ledgerTransactions.Funds[x.FundId].Item2,
                    x.Value,
                    null,
                    x.Date,
                    null,
                    null,
                    x.Title,
                    x.Comment
            ))],

            Deallocations = [.. ledgerTransactions.FundTransactions
                .Where(x => x.Value.Amount < 0 && x.InTransfer == null && x.OutTransfer == null)
                .Select(x => new LedgerTransactionsDTO.FundTransaction(
                    x.Id,
                    ledgerTransactions.Funds[x.FundId].Item1,
                    ledgerTransactions.Budgets[ledgerTransactions.Funds[x.FundId].Item1],
                    x.FundId,
                    ledgerTransactions.Funds[x.FundId].Item2,
                    x.Value,
                    null,
                    x.Date,
                    null,
                    null,
                    x.Title,
                    x.Comment
            ))],

            Reallocations = [.. reallocations
                .Select(x => new LedgerTransactionsDTO.FundTransaction(
                    x.Transfer.Id,
                    ledgerTransactions.Funds[x.Deallocation.FundId].Item1,
                    ledgerTransactions.Budgets[ledgerTransactions.Funds[x.Deallocation.FundId].Item1],
                    x.Deallocation.FundId,
                    ledgerTransactions.Funds[x.Deallocation.FundId].Item2,
                    x.Allocation.Value,
                    null,
                    x.Allocation.Date,
                    x.Allocation.FundId,
                    ledgerTransactions.Funds[x.Allocation.FundId].Item2,
                    x.Allocation.Title,
                    x.Allocation.Comment
            ))],
        };
    }
}
