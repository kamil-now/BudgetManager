using BudgetManager.Application.Interfaces;
using BudgetManager.Application.Models;
using BudgetManager.Application.Queries;

namespace BudgetManager.Application.Handlers;

public sealed class GetLedgerAllocationTemplatesHandler(ILedgerAllocationTemplateReader templateReader)
    : IRequestHandler<GetLedgerAllocationTemplatesQuery, IEnumerable<LedgerAllocationTemplateDTO>>
{
    public async Task<IEnumerable<LedgerAllocationTemplateDTO>> Handle(GetLedgerAllocationTemplatesQuery query, CancellationToken cancellationToken)
    {
        var templates = await templateReader.ReadAsync(query.LedgerId, cancellationToken);

        return [.. templates.Select(template => new LedgerAllocationTemplateDTO(
            template.Id,
            template.Name,
            template.Currency,
            template.MinimumAmount(),
            [.. template.Lines.Select(x => new LedgerAllocationTemplateDTO.Line(x.BudgetId, x.BudgetName, x.Sequence, x.Type, x.Amount, x.Percent))]))];
    }
}
