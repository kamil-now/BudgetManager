using BudgetManager.Application.Interfaces;
using BudgetManager.Application.Models;
using BudgetManager.Application.Queries;

namespace BudgetManager.Application.Handlers;

public sealed class GetAllocationTemplatesHandler(IAllocationTemplateReader templateReader)
    : IRequestHandler<GetAllocationTemplatesQuery, IEnumerable<AllocationTemplateDTO>>
{
    public async Task<IEnumerable<AllocationTemplateDTO>> Handle(GetAllocationTemplatesQuery query, CancellationToken cancellationToken)
    {
        var templates = await templateReader.ReadAsync(query.BudgetId, cancellationToken);

        return [.. templates.Select(template => new AllocationTemplateDTO(
            template.Id,
            template.Name,
            template.Currency,
            template.MinimumAmount(),
            [.. template.Lines
                .OrderBy(x => x.Sequence)
                .Select(x => new AllocationTemplateDTO.Line(x.FundId, x.FundName, x.Sequence, x.Type, x.Amount, x.Percent))]))];
    }
}
