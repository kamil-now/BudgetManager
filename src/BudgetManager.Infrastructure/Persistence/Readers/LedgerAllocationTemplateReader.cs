using BudgetManager.Application.Interfaces;
using BudgetManager.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BudgetManager.Infrastructure.Persistence.Readers;

public sealed class LedgerAllocationTemplateReader(ApplicationDbContext dbContext) : ILedgerAllocationTemplateReader
{
    public async Task<IEnumerable<LedgerAllocationTemplateDetails>> ReadAsync(Guid ledgerId, CancellationToken cancellationToken = default)
    {
        var templates = await dbContext.LedgerAllocationTemplates
            .AsNoTracking()
            .Where(x => x.LedgerId == ledgerId)
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Currency,
                Lines = x.Lines.Select(line => new LedgerAllocationTemplateDetails.Line(
                    line.BudgetId,
                    line.Budget.Name,
                    line.Sequence,
                    line.Type,
                    line.Amount,
                    line.Percent)).ToList()
            })
            .ToArrayAsync(cancellationToken);

        return [.. templates.Select(template => new LedgerAllocationTemplateDetails(
            template.Id,
            template.Name,
            template.Currency,
            [.. template.Lines.OrderBy(x => x.Sequence)]))];
    }
}
