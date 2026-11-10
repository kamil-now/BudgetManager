using BudgetManager.Domain.Models;

namespace BudgetManager.Infrastructure.Persistence.Readers;

internal sealed record BalanceRow(Guid OwnerId, string Currency, decimal Amount);

internal static class BalanceRows
{
    public static Dictionary<Guid, Balance> ToBalances(IEnumerable<BalanceRow> rows)
    {
        var balances = new Dictionary<Guid, Balance>();
        foreach (var row in rows)
        {
            if (!balances.TryGetValue(row.OwnerId, out var balance))
            {
                balance = [];
                balances[row.OwnerId] = balance;
            }
            balance.Add(new Money(row.Amount, row.Currency));
        }
        return balances;
    }

    public static Balance BalanceOf(Dictionary<Guid, Balance> balances, Guid ownerId)
        => balances.TryGetValue(ownerId, out var balance) ? balance : [];
}
