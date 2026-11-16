using BudgetManager.Application.Commands;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class DeleteAllocationHandler(IEntityStore store) : IRequestHandler<DeleteAllocationCommand>
{
    public async Task Handle(DeleteAllocationCommand command, CancellationToken cancellationToken)
    {
        var allocation = (await store.GetAsync<Allocation>(
            x => x.Id == command.Id && x.LedgerId == command.LedgerId,
            cancellationToken)).SingleOrDefault()
            ?? throw new NotFoundException($"Allocation {command.Id} not found.");

        // its fund transactions are deleted by the database (cascade)
        await store.DeleteAsync<Allocation>(allocation.Id, cancellationToken);

        await store.SaveChangesAsync(cancellationToken);
    }
}
