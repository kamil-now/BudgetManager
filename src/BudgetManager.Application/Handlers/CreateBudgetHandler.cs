using BudgetManager.Application.Commands;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class CreateBudgetHandler(IEntityStore store) : IRequestHandler<CreateBudgetCommand, Guid>
{
    public async Task<Guid> Handle(CreateBudgetCommand command, CancellationToken cancellationToken)
    {
        await ValidateCommandAsync(command, cancellationToken);

        var budget = new Budget
        {
            LedgerId = command.LedgerId,
            Name = command.Name
        };
        budget.AddFundsAndTemplates(command.Funds, command.AllocationTemplates);

        var entity = await store.CreateAsync(budget, cancellationToken) ?? throw new InvalidOperationException("Failed to create budget.");

        await store.SaveChangesAsync(cancellationToken);

        if (entity.Id == Guid.Empty)
        {
            throw new InvalidOperationException("Budget ID cannot be empty.");
        }
        return entity.Id;
    }

    private async Task ValidateCommandAsync(CreateBudgetCommand command, CancellationToken cancellationToken)
    {
        command.LedgerId.EnsureNotEmpty();
        command.Name.EnsureNotEmpty("Budget name").EnsureNotLongerThan(Constants.MaxNameLength, "Budget name");
        command.Funds.EnsureNotEmpty().EnsureValidFunds();
        command.AllocationTemplates.EnsureValidAllocationTemplates(command.Funds);

        if (await store.ExistsAsync<Budget>(x => x.Name == command.Name && x.LedgerId == command.LedgerId, cancellationToken))
        {
            throw new ConflictException($"Budget with name {command.Name} already exists in ledger {command.LedgerId}.");
        }
    }
}
