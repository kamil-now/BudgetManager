using BudgetManager.Application.Validators;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;
using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Handlers;

internal static class LedgerAllocationTemplates
{
    public static async Task EnsureBudgetsOfLedgerAsync(
        IEntityStore store,
        IEnumerable<LedgerAllocationLine> lines,
        Guid ledgerId,
        CancellationToken cancellationToken)
    {
        var budgetIds = (await store.GetAsync<Budget>(x => x.LedgerId == ledgerId, cancellationToken))
            .Select(x => x.Id)
            .ToHashSet();
        if (lines.Any(x => !budgetIds.Contains(x.BudgetId)))
        {
            throw new ValidationException("Allocation lines must allocate to budgets of the ledger.");
        }
    }
}
