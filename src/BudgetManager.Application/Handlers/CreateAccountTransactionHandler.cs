using BudgetManager.Application.Commands;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;
using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Handlers;

public sealed class CreateAccountTransactionHandler(IEntityStore store) : IRequestHandler<CreateAccountTransactionCommand, Guid>
{
    public async Task<Guid> Handle(CreateAccountTransactionCommand command, CancellationToken cancellationToken)
    {
        ValidateCommand(command);

        IReadOnlyList<FundAmount> allocations = [];
        if (command.Allocation is not null || command.FundId is not null)
        {
            var account = await store.GetAsync<Account>(command.AccountId, cancellationToken);
            if (command.Allocation is not null)
            {
                allocations = await IncomeAllocation.SplitAsync(store, command.Allocation, command.Value, account.LedgerId, cancellationToken);
            }
            if (command.FundId is { } fundId)
            {
                await ExpenseFund.EnsureInLedgerAsync(store, fundId, account.LedgerId, cancellationToken);
            }
        }

        var entity = await store.CreateAsync(
            AccountTransaction.Create(command.AccountId, command.Value, command.Date, command.Title),
            cancellationToken) ?? throw new InvalidOperationException("Failed to create AccountOperation.");

        foreach (var allocation in allocations.ToFundTransactions(entity))
        {
            await store.CreateAsync(allocation, cancellationToken);
        }
        if (command.FundId is { } payingFundId)
        {
            await store.CreateAsync(FundTransaction.Pay(payingFundId, entity), cancellationToken);
        }

        await store.SaveChangesAsync(cancellationToken);

        if (entity.Id == Guid.Empty)
        {
            throw new InvalidOperationException("AccountOperation ID cannot be empty.");
        }
        return entity.Id;
    }

    private static void ValidateCommand(CreateAccountTransactionCommand command)
    {
        command.Value.EnsureValid();
        command.Title?.EnsureNotLongerThan(Constants.MaxTitleLength);
        command.FundId.EnsureValidExpenseFund(command.Value, command.Allocation);
        command.Allocation?.EnsureValidAllocation(command.Value);
    }
}
