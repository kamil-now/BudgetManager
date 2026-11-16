using BudgetManager.Application.Commands;
using BudgetManager.Application.Interfaces;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;
using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Handlers;

public sealed class UpdateAllocationHandler(IEntityStore store, IUnallocatedReader unallocatedReader) : IRequestHandler<UpdateAllocationCommand>
{
    public async Task Handle(UpdateAllocationCommand command, CancellationToken cancellationToken)
    {
        ValidateCommand(command);

        var allocation = await store.GetAsync<Allocation>(command.Id, [x => x.Transactions], cancellationToken);
        if (allocation.LedgerId != command.LedgerId)
        {
            throw new NotFoundException($"Allocation {command.Id} not found.");
        }

        if (allocation.IsInitial && command.Date != allocation.Date)
        {
            throw new ValidationException("Initial allocation date cannot be changed.");
        }

        // what the allocation holds now is unallocated again once it is replaced, but only in its own currency
        var unallocated = await unallocatedReader.ReadAsync(command.LedgerId, command.Currency, cancellationToken)
            + (allocation.Currency == command.Currency ? allocation.Total : 0);
        if (unallocated <= 0)
        {
            throw new ValidationException($"Nothing to allocate in {command.Currency}.");
        }
        if (command.Amount > unallocated)
        {
            throw new ValidationException(FormattableString.Invariant(
                $"Amount {command.Amount} cannot exceed the unallocated {unallocated} {command.Currency}."));
        }

        var amount = new Money(command.Amount, command.Currency);
        command.Allocation.EnsureValidAllocation(amount);

        var amounts = await IncomeAllocation.SplitAsync(store, command.Allocation, amount, command.LedgerId, cancellationToken);
        if (amounts.Count == 0)
        {
            throw new ValidationException("Allocation does not allocate anything to funds.");
        }

        foreach (var transaction in allocation.Transactions)
        {
            await store.DeleteAsync<FundTransaction>(transaction.Id, cancellationToken);
        }

        allocation.Update(command.Currency, command.Date);
        foreach (var fundAmount in amounts)
        {
            await store.CreateAsync(
                FundTransaction.Allocate(fundAmount.FundId, new Money(fundAmount.Amount, command.Currency), allocation),
                cancellationToken);
        }

        await store.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateCommand(UpdateAllocationCommand command)
    {
        command.Currency.EnsureValidCurrency();

        if (command.Amount <= 0)
        {
            throw new ValidationException("Amount must be positive.");
        }
        command.Amount.EnsureValidAmount("Amount");

        if (command.Allocation is null)
        {
            throw new ValidationException("Allocation cannot be empty.");
        }
    }
}
