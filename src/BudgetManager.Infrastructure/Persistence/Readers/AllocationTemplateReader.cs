using BudgetManager.Application.Interfaces;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BudgetManager.Infrastructure.Persistence.Readers;

public sealed class AllocationTemplateReader(ApplicationDbContext dbContext) : IAllocationTemplateReader
{
    public async Task<IEnumerable<AllocationTemplateDetails>> ReadAsync(Guid budgetId, CancellationToken cancellationToken = default)
    {
        var templates = await dbContext.AllocationTemplates
            .AsNoTracking()
            .Where(x => x.BudgetId == budgetId)
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Currency)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Currency,
                Lines = x.Lines.Select(line => new
                {
                    line.FundId,
                    FundName = line.Fund.Name,
                    line.Sequence,
                    Amount = line is FixedAllocationTemplateLine ? (decimal?)((FixedAllocationTemplateLine)line).Amount : null,
                    Percent = line is PercentAllocationTemplateLine ? (decimal?)((PercentAllocationTemplateLine)line).Percent : null
                }).ToList()
            })
            .ToArrayAsync(cancellationToken);

        return [.. templates.Select(template => new AllocationTemplateDetails(
            template.Id,
            template.Name,
            template.Currency,
            [.. template.Lines
                .OrderBy(x => x.Sequence)
                .Select(x => new AllocationTemplateDetails.Line(
                    x.FundId,
                    x.FundName,
                    x.Sequence,
                    x.Percent is null ? AllocationType.Fixed : AllocationType.Percent,
                    x.Amount,
                    x.Percent))]))];
    }
}
