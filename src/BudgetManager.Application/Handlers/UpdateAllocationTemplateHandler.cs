using BudgetManager.Application.Commands;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class UpdateAllocationTemplateHandler(IEntityStore store) : IRequestHandler<UpdateAllocationTemplateCommand>
{
    public async Task Handle(UpdateAllocationTemplateCommand command, CancellationToken cancellationToken)
    {
        var template = await store.GetAsync<AllocationTemplate>(command.TemplateId, [x => x.Lines], cancellationToken);
        if (template.BudgetId != command.BudgetId)
        {
            throw new NotFoundException($"Allocation template {command.TemplateId} not found.");
        }

        command.Name.EnsureNotEmpty("Allocation template name").EnsureNotLongerThan(Constants.MaxNameLength, "Allocation template name");
        command.Lines.EnsureValidAllocationLines(template.Currency);

        var fundIds = (await store.GetAsync<Fund>(x => x.BudgetId == command.BudgetId, cancellationToken))
            .Select(x => x.Id)
            .ToHashSet();
        if (command.Lines.Any(x => !fundIds.Contains(x.FundId)))
        {
            throw new ValidationException("Allocation lines must allocate to funds of the budget.");
        }

        if (await store.ExistsAsync<AllocationTemplate>(
            x => x.BudgetId == command.BudgetId && x.Name == command.Name && x.Id != command.TemplateId,
            cancellationToken))
        {
            throw new ConflictException($"Allocation template with name {command.Name} already exists in budget {command.BudgetId}.");
        }

        template.Name = command.Name;
        foreach (var line in template.ReplaceLines(command.Lines))
        {
            await store.CreateAsync(line, cancellationToken);
        }

        await store.SaveChangesAsync(cancellationToken);
    }
}
