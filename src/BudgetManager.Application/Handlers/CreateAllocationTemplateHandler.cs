using BudgetManager.Application.Commands;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class CreateAllocationTemplateHandler(IEntityStore store) : IRequestHandler<CreateAllocationTemplateCommand, Guid>
{
    public async Task<Guid> Handle(CreateAllocationTemplateCommand command, CancellationToken cancellationToken)
    {
        command.BudgetId.EnsureNotEmpty();
        command.Name.EnsureNotEmpty("Allocation template name").EnsureNotLongerThan(Constants.MaxNameLength, "Allocation template name");
        command.Lines.EnsureValidAllocationLines(command.Currency);

        await EnsureFundsOfBudgetAsync(command, cancellationToken);

        if (await store.ExistsAsync<AllocationTemplate>(x => x.BudgetId == command.BudgetId && x.Name == command.Name, cancellationToken))
        {
            throw new ConflictException($"Allocation template with name {command.Name} already exists in budget {command.BudgetId}.");
        }

        var template = await store.CreateAsync(
            AllocationTemplate.Create(command.BudgetId, command.Name, command.Currency, command.Lines),
            cancellationToken);

        await store.SaveChangesAsync(cancellationToken);

        return template.Id;
    }

    private async Task EnsureFundsOfBudgetAsync(CreateAllocationTemplateCommand command, CancellationToken cancellationToken)
    {
        var fundIds = (await store.GetAsync<Fund>(x => x.BudgetId == command.BudgetId, cancellationToken))
            .Select(x => x.Id)
            .ToHashSet();
        if (command.Lines.Any(x => !fundIds.Contains(x.FundId)))
        {
            throw new ValidationException("Allocation lines must allocate to funds of the budget.");
        }
    }
}
