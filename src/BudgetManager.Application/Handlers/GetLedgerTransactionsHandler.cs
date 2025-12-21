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
        await query.LedgerId.EnsureAccessibleAsync<Ledger>(currentUser, service, cancellationToken);

        var filters = query.Filters;
        filters.From ??= DateTimeOffset.MinValue;
        filters.To ??= DateTimeOffset.MaxValue;

        var transactions = await service.GetLedgerTransactionsAsync(query.LedgerId, filters, cancellationToken);

        return new LedgerTransactionsDTO()
        {
            Incomes = [.. transactions.Incomes.Select(x => new LedgerTransactionsDTO.AccountTransaction(
                x.Id,
                x.AccountId,
                transactions.Accounts[x.AccountId],
                x.Amount,
                x.Date,
                null,
                null,
                x.Title,
                x.Comment,
                x.Tags
            ))],
            Expenses = [..transactions.Expenses.Select(x => new LedgerTransactionsDTO.AccountTransaction(
                x.Id,
                x.AccountId,
                transactions.Accounts[x.AccountId],
                x.Amount,
                x.Date,
                null,
                null,
                x.Title,
                x.Comment,
                x.Tags
            ))],
            Transfers = [..transactions.Transfers.Select(x => new LedgerTransactionsDTO.AccountTransaction(
                x.Id,
                x.AccountId,
                transactions.Accounts[x.AccountId],
                x.Amount,
                x.Date,
                x.TargetAccountId,
                transactions.Accounts[x.TargetAccountId],
                x.Title,
                x.Comment,
                x.Tags
            ))],

            Allocations = [..transactions.Allocations.Select(x => new LedgerTransactionsDTO.BudgetTransaction(
                x.Id,
                transactions.Funds[x.FundId].Item1,
                transactions.Budgets[transactions.Funds[x.FundId].Item1],
                x.FundId,
                transactions.Funds[x.FundId].Item2,
                x.Amount,
                x.Date,
                null,
                null,
                x.Title,
                x.Comment
            ))],
            Deallocations = [.. transactions.Deallocations.Select(x => new LedgerTransactionsDTO.BudgetTransaction(
                x.Id,
                transactions.Funds[x.FundId].Item1,
                transactions.Budgets[transactions.Funds[x.FundId].Item1],
                x.FundId,
                transactions.Funds[x.FundId].Item2,
                x.Amount,
                x.Date,
                null,
                null,
                x.Title,
                x.Comment
            ))],
            Reallocations = [.. transactions.Reallocations.Select(x => new LedgerTransactionsDTO.BudgetTransaction(
                x.Id,
                transactions.Funds[x.FundId].Item1,
                transactions.Budgets[transactions.Funds[x.FundId].Item1],
                x.FundId,
                transactions.Funds[x.FundId].Item2,
                x.Amount,
                x.Date,
                transactions.Funds[x.TargetFundId].Item1,
                transactions.Budgets[transactions.Funds[x.TargetFundId].Item1],
                x.Title,
                x.Comment
            ))]
        };
    }
}
