using BudgetManager.Application.Interfaces;
using BudgetManager.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BudgetManager.Infrastructure.Persistence.Readers;

public sealed class FundLabelReader(ApplicationDbContext dbContext) : IFundLabelReader
{
    public async Task<IReadOnlyDictionary<Guid, FundLabel>> ReadAsync(IEnumerable<Guid> fundIds, CancellationToken cancellationToken = default)
    {
        var ids = fundIds.Distinct().ToArray();

        return await dbContext.Funds
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .Select(x => new FundLabel(x.Id, x.Name, x.BudgetId, x.Budget.Name))
            .ToDictionaryAsync(x => x.FundId, cancellationToken);
    }
}
