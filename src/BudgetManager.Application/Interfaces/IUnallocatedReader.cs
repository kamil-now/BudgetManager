namespace BudgetManager.Application.Interfaces;

public interface IUnallocatedReader
{
    /// <summary>
    /// The ledger's money in <paramref name="currency"/> not allocated to any fund: its account total minus its fund total.
    /// </summary>
    Task<decimal> ReadAsync(Guid ledgerId, string currency, CancellationToken cancellationToken = default);
}
