using BudgetManager.Application.Commands;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class CreateLedgerAllocationTemplateHandler(IEntityStore store) : IRequestHandler<CreateLedgerAllocationTemplateCommand, Guid>
{
    public async Task<Guid> Handle(CreateLedgerAllocationTemplateCommand command, CancellationToken cancellationToken)
    {
        command.LedgerId.EnsureNotEmpty();
        command.Name.EnsureNotEmpty("Allocation template name").EnsureNotLongerThan(Constants.MaxNameLength, "Allocation template name");
        var lines = command.Lines.EnsureValidLedgerAllocationLines(command.Currency);

        await LedgerAllocationTemplates.EnsureBudgetsOfLedgerAsync(store, lines, command.LedgerId, cancellationToken);

        if (await store.ExistsAsync<LedgerAllocationTemplate>(x => x.LedgerId == command.LedgerId && x.Name == command.Name, cancellationToken))
        {
            throw new ConflictException($"Allocation template with name {command.Name} already exists in ledger {command.LedgerId}.");
        }

        var template = await store.CreateAsync(
            LedgerAllocationTemplate.Create(command.LedgerId, command.Name, command.Currency, lines),
            cancellationToken);

        await store.SaveChangesAsync(cancellationToken);

        return template.Id;
    }
}
