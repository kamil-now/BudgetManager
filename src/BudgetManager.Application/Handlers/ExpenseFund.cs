using BudgetManager.Application.Validators;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

internal static class ExpenseFund
{
    public static async Task EnsureInLedgerAsync(IEntityStore store, Guid fundId, Guid ledgerId, CancellationToken cancellationToken)
    {
        if (!await store.ExistsAsync<Fund>(x => x.Id == fundId && x.Budget.LedgerId == ledgerId, cancellationToken))
        {
            throw new ValidationException("Fund must belong to a budget in the account's ledger.");
        }
    }
}
