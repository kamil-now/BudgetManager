using BudgetManager.Application.Commands;
using BudgetManager.Application.Interfaces;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;
using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Handlers;

public sealed class AllocateFundsHandler(IEntityStore store, IUnallocatedReader unallocatedReader) : IRequestHandler<AllocateFundsCommand, Guid>
{
    public async Task<Guid> Handle(AllocateFundsCommand command, CancellationToken cancellationToken)
    {
        ValidateCommand(command);

        var unallocated = await unallocatedReader.ReadAsync(command.LedgerId, command.Currency, cancellationToken);
        if (unallocated <= 0)
        {
            throw new ValidationException($"Nothing to allocate in {command.Currency}.");
        }

        var amount = new Money(command.Amount ?? unallocated, command.Currency);
        if (amount.Amount > unallocated)
        {
            throw new ValidationException(FormattableString.Invariant(
                $"Amount {amount.Amount} cannot exceed the unallocated {unallocated} {command.Currency}."));
        }

        command.Allocation.EnsureValidAllocation(amount);

        var amounts = await IncomeAllocation.SplitAsync(store, command.Allocation, amount, command.LedgerId, cancellationToken);
        if (amounts.Count == 0)
        {
            throw new ValidationException("Allocation does not allocate anything to funds.");
        }

        var date = command.Date ?? new DateTimeOffset(DateTimeOffset.UtcNow.UtcDateTime.Date, TimeSpan.Zero);

        var allocation = await store.CreateAsync(
            Allocation.Create(command.LedgerId, command.Currency, date, amounts),
            cancellationToken);

        await store.SaveChangesAsync(cancellationToken);

        return allocation.Id;
    }

    private static void ValidateCommand(AllocateFundsCommand command)
    {
        command.Currency.EnsureValidCurrency();

        if (command.Amount is { } amount)
        {
            if (amount <= 0)
            {
                throw new ValidationException("Amount must be positive.");
            }
            amount.EnsureValidAmount("Amount");
        }

        if (command.Allocation is null)
        {
            throw new ValidationException("Allocation cannot be empty.");
        }
    }
}
