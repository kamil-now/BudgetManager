using BudgetManager.Application.Commands;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class UpdateLedgerAllocationTemplateHandler(IEntityStore store) : IRequestHandler<UpdateLedgerAllocationTemplateCommand>
{
    public async Task Handle(UpdateLedgerAllocationTemplateCommand command, CancellationToken cancellationToken)
    {
        var template = await store.GetAsync<LedgerAllocationTemplate>(command.TemplateId, [x => x.Lines], cancellationToken);
        if (template.LedgerId != command.LedgerId)
        {
            throw new NotFoundException($"Allocation template {command.TemplateId} not found.");
        }

        command.Name.EnsureNotEmpty("Allocation template name").EnsureNotLongerThan(Constants.MaxNameLength, "Allocation template name");
        var lines = command.Lines.EnsureValidLedgerAllocationLines(template.Currency);

        await LedgerAllocationTemplates.EnsureBudgetsOfLedgerAsync(store, lines, command.LedgerId, cancellationToken);

        if (await store.ExistsAsync<LedgerAllocationTemplate>(
            x => x.LedgerId == command.LedgerId && x.Name == command.Name && x.Id != command.TemplateId,
            cancellationToken))
        {
            throw new ConflictException($"Allocation template with name {command.Name} already exists in ledger {command.LedgerId}.");
        }

        template.Name = command.Name;
        foreach (var line in template.ReplaceLines(lines))
        {
            await store.CreateAsync(line, cancellationToken);
        }

        await store.SaveChangesAsync(cancellationToken);
    }
}
