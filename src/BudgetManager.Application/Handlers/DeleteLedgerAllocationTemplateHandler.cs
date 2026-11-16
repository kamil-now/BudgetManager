using BudgetManager.Application.Commands;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class DeleteLedgerAllocationTemplateHandler(IEntityStore store) : IRequestHandler<DeleteLedgerAllocationTemplateCommand>
{
    public async Task Handle(DeleteLedgerAllocationTemplateCommand command, CancellationToken cancellationToken)
    {
        var template = (await store.GetAsync<LedgerAllocationTemplate>(
            x => x.Id == command.TemplateId && x.LedgerId == command.LedgerId,
            cancellationToken)).SingleOrDefault()
            ?? throw new NotFoundException($"Allocation template {command.TemplateId} not found.");

        await store.DeleteAsync<LedgerAllocationTemplate>(template.Id, cancellationToken);

        await store.SaveChangesAsync(cancellationToken);
    }
}
