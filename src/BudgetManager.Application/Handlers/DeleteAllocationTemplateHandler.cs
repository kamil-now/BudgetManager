using BudgetManager.Application.Commands;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class DeleteAllocationTemplateHandler(IEntityStore store) : IRequestHandler<DeleteAllocationTemplateCommand>
{
    public async Task Handle(DeleteAllocationTemplateCommand command, CancellationToken cancellationToken)
    {
        var template = (await store.GetAsync<AllocationTemplate>(
            x => x.Id == command.TemplateId && x.BudgetId == command.BudgetId,
            cancellationToken)).SingleOrDefault()
            ?? throw new NotFoundException($"Allocation template {command.TemplateId} not found.");

        await store.DeleteAsync<AllocationTemplate>(template.Id, cancellationToken);

        await store.SaveChangesAsync(cancellationToken);
    }
}
