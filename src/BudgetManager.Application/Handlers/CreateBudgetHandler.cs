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

        var budgetId = Guid.NewGuid();
        var entity = await store.CreateAsync(new Budget
        {
            Id = budgetId,
            LedgerId = command.LedgerId,
            Name = command.Name,
            Description = command.Description,
            Funds = [.. command.Funds.Select(x => new Fund()
            {
                BudgetId = budgetId,
                Name = x.Name,
                Description = x.Description,
                AllocationTemplateSequence = x.AllocationTemplateSequence,
                AllocationTemplateType = x.AllocationTemplateType,
                AllocationTemplateValue = x.AllocationTemplateValue
            })]
        }, cancellationToken) ?? throw new InvalidOperationException("Failed to create budget.");

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
        command.Description?.EnsureNotLongerThan(Constants.MaxCommentLength);
        command.Funds.EnsureNotEmpty();

        foreach (var fund in command.Funds)
        {
            fund.Name.EnsureNotEmpty("Fund name").EnsureNotLongerThan(Constants.MaxNameLength, "Fund name");
            fund.Description?.EnsureNotLongerThan(Constants.MaxCommentLength, $"Description of {fund.Name}");
            fund.AllocationTemplateSequence.EnsureNonnegative($"AllocationTemplateSequence of {fund.Name}");
        }

        if (await store.ExistsAsync<Budget>(x => x.Name == command.Name && x.LedgerId == command.LedgerId, cancellationToken))
        {
            throw new ConflictException($"Budget with name {command.Name} already exists in ledger {command.LedgerId}.");
        }
    }
}
