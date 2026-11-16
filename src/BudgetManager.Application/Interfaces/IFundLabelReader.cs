using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Interfaces;

public interface IFundLabelReader
{
    /// <summary>
    /// The labels of the given funds that exist, by fund id.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, FundLabel>> ReadAsync(IEnumerable<Guid> fundIds, CancellationToken cancellationToken = default);
}
