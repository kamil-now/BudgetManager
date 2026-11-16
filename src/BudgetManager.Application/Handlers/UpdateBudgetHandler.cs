using BudgetManager.Application.Commands;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class UpdateBudgetHandler(IEntityStore store) : IRequestHandler<UpdateBudgetCommand>
{
    public async Task Handle(UpdateBudgetCommand command, CancellationToken cancellationToken)
    {
        command.Id.EnsureNotEmpty();
        command.Name.EnsureNotEmpty("Budget name").EnsureNotLongerThan(Constants.MaxNameLength, "Budget name");
        var funds = command.Funds.EnsureValidFunds();

        var budget = await store.GetAsync<Budget>(command.Id, [x => x.Funds], cancellationToken);

        var existing = budget.Funds.ToDictionary(x => x.Id);
        if (funds.FirstOrDefault(x => x.Id is { } id && !existing.ContainsKey(id)) is { } unknown)
        {
            throw new ValidationException($"Fund {unknown.Id} is not a fund of budget {budget.Id}.");
        }

        if (await store.ExistsAsync<Budget>(
            x => x.LedgerId == budget.LedgerId && x.Name == command.Name && x.Id != command.Id,
            cancellationToken))
        {
            throw new ConflictException($"Budget with name {command.Name} already exists in ledger {budget.LedgerId}.");
        }

        budget.Name = command.Name;

        var kept = funds.Where(x => x.Id is not null).Select(x => x.Id!.Value).ToHashSet();
        var removed = existing.Keys.Where(x => !kept.Contains(x)).ToArray();
        await RemoveFundsAsync(budget, removed, cancellationToken);

        foreach (var fund in funds)
        {
            if (fund.Id is { } id)
            {
                existing[id].Name = fund.Name;
            }
            else
            {
                // the id is set at construction, so a fund found only through Funds would be taken for an existing row
                await store.CreateAsync(budget.AddFund(fund.Name), cancellationToken);
            }
        }

        await store.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Deletes the funds with their rows, which deallocates what they hold, then the allocations and allocation templates
    /// that had rows or lines only in those funds.
    /// </summary>
    private async Task RemoveFundsAsync(Budget budget, Guid[] fundIds, CancellationToken cancellationToken)
    {
        if (fundIds.Length == 0)
        {
            return;
        }

        var allocations = await store.GetAsync<Allocation>(
            x => x.LedgerId == budget.LedgerId
                && x.Transactions.Any(t => fundIds.Contains(t.FundId))
                && x.Transactions.All(t => fundIds.Contains(t.FundId)),
            cancellationToken);
        foreach (var allocation in allocations)
        {
            await store.DeleteAsync<Allocation>(allocation.Id, cancellationToken);
        }

        var templates = await store.GetAsync<AllocationTemplate>(
            x => x.BudgetId == budget.Id
                && x.Lines.Any(l => fundIds.Contains(l.FundId))
                && x.Lines.All(l => fundIds.Contains(l.FundId)),
            cancellationToken);
        foreach (var template in templates)
        {
            await store.DeleteAsync<AllocationTemplate>(template.Id, cancellationToken);
        }

        foreach (var fundId in fundIds)
        {
            await store.DeleteAsync<Fund>(fundId, cancellationToken);
        }
    }
}
