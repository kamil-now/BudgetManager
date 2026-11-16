using BudgetManager.Application.Commands;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Interfaces;
using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Handlers;

public sealed class UpdateAccountTransactioHandler(IEntityStore store) : IRequestHandler<UpdateAccountTransactionCommand>
{
    public async Task Handle(UpdateAccountTransactionCommand command, CancellationToken cancellationToken)
    {
        ValidateCommand(command);

        var transaction = await store.GetAsync<AccountTransaction>(command.Id, cancellationToken);

        if (transaction.IsTransfer)
        {
            throw new ValidationException("Transaction is part of a transfer and can only be changed through the transfer.");
        }

        if (transaction.AccountId != command.AccountId)
        {
            if (transaction.Type == AccountTransactionType.InitialBalance)
            {
                throw new ValidationException("Initial balance cannot be moved to another account.");
            }

            var currentAccount = await store.GetAsync<Account>(transaction.AccountId, cancellationToken);
            var targetAccount = await store.GetAsync<Account>(command.AccountId, cancellationToken);

            if (currentAccount.LedgerId != targetAccount.LedgerId)
            {
                throw new ValidationException("Transaction target account must belong to the same ledger as the current account.");
            }
        }

        if (transaction.Type == AccountTransactionType.InitialBalance)
        {
            if (command.Date != transaction.Date)
            {
                throw new ValidationException("Initial balance date cannot be changed.");
            }
            if (command.FundId is not null)
            {
                throw new ValidationException("Initial balance cannot be paid from a fund.");
            }
        }

        var linked = (await store.GetAsync<FundTransaction>(x => x.AccountTransactionId == transaction.Id, cancellationToken)).ToArray();
        // positive rows are the allocations of an income, a negative one is the payment of an expense
        var existing = linked.Where(x => x.Value.Amount > 0).ToArray();
        var payments = linked.Where(x => x.Value.Amount < 0).ToArray();
        var allocations = await SplitAsync(command, transaction, existing, cancellationToken);

        if (command.FundId is { } fundId)
        {
            var account = await store.GetAsync<Account>(command.AccountId, cancellationToken);
            await ExpenseFund.EnsureInLedgerAsync(store, fundId, account.LedgerId, cancellationToken);
        }

        transaction.Update(command.AccountId, command.Amount, command.Date, command.Title);

        foreach (var payment in payments)
        {
            await store.DeleteAsync<FundTransaction>(payment.Id, cancellationToken);
        }
        if (command.FundId is { } payingFundId)
        {
            await store.CreateAsync(FundTransaction.Pay(payingFundId, transaction), cancellationToken);
        }

        if (allocations is null)
        {
            foreach (var allocation in existing)
            {
                allocation.Date = transaction.Date;
                allocation.Title = transaction.Title;
            }
        }
        else
        {
            foreach (var allocation in existing)
            {
                await store.DeleteAsync<FundTransaction>(allocation.Id, cancellationToken);
            }
            foreach (var allocation in allocations.ToFundTransactions(transaction))
            {
                await store.CreateAsync(allocation, cancellationToken);
            }
        }

        await store.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// The new allocations of the transaction, or null when the existing ones are kept.
    /// </summary>
    private async Task<IReadOnlyList<FundAmount>?> SplitAsync(
        UpdateAccountTransactionCommand command,
        AccountTransaction transaction,
        FundTransaction[] existing,
        CancellationToken cancellationToken)
    {
        if (command.Allocation is null)
        {
            if (existing.Length > 0
                && (existing.Any(x => x.Value.Currency != command.Amount.Currency)
                    || command.Amount.Amount <= 0
                    || command.Amount.Amount < existing.Sum(x => x.Value.Amount)))
            {
                throw new ValidationException("Transaction has fund allocations; send a new allocation.");
            }
            return null;
        }

        if (!command.Allocation.AllocatesAnything)
        {
            return [];
        }

        if (transaction.Type == AccountTransactionType.InitialBalance)
        {
            throw new ValidationException("Initial balance cannot be allocated to funds.");
        }

        var account = await store.GetAsync<Account>(command.AccountId, cancellationToken);
        return await IncomeAllocation.SplitAsync(store, command.Allocation, command.Amount, account.LedgerId, cancellationToken);
    }

    private static void ValidateCommand(UpdateAccountTransactionCommand command)
    {
        command.Amount.EnsureValid();
        command.Title?.EnsureNotLongerThan(Constants.MaxTitleLength);
        command.FundId.EnsureValidExpenseFund(command.Amount, command.Allocation);
        command.Allocation?.EnsureValidAllocation(command.Amount, allowClear: true);
    }
}
