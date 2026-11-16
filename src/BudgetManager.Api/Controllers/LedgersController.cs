using BudgetManager.Application.Commands;
using BudgetManager.Application.Queries;
using BudgetManager.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetManager.Api.Controllers;

[Authorize]
public class LedgersController(IMediator mediator) : BaseController
{
    [HttpPost]
    public async Task<ActionResult> CreateLedger([FromBody] CreateLedgerCommand request)
    {
        var id = await mediator.Send(request);
        return Ok(id);
    }

    [HttpGet]
    public async Task<ActionResult> GetLedgers()
    {
        var ledgers = await mediator.Send(new GetLedgersQuery());
        return Ok(ledgers);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> GetLedger([FromRoute] Guid id)
    {
        var ledger = await mediator.Send(new GetLedgerQuery(id));
        return Ok(ledger);
    }

    [HttpGet("{id}/feed")]
    public async Task<ActionResult> GetLedgerFeed([FromRoute] Guid id, [FromQuery] LedgerFeedFilters filters)
    {
        var feed = await mediator.Send(new GetLedgerFeedQuery(id, filters));
        return Ok(feed);
    }

    [HttpPost("{id}/allocations")]
    public async Task<ActionResult> AllocateFunds([FromRoute] Guid id, [FromBody] AllocateFundsCommand request)
    {
        var allocationId = await mediator.Send(request with { LedgerId = id });
        return Ok(allocationId);
    }

    [HttpPut("{id}/allocations/{allocationId}")]
    public async Task<ActionResult> UpdateAllocation([FromRoute] Guid id, [FromRoute] Guid allocationId, [FromBody] UpdateAllocationCommand request)
    {
        await mediator.Send(request with { LedgerId = id, Id = allocationId });
        return Ok();
    }

    [HttpPost("{id}/allocation-preview")]
    public async Task<ActionResult> GetAllocationPreview([FromRoute] Guid id, [FromBody] GetLedgerAllocationPreviewQuery request)
    {
        var preview = await mediator.Send(request with { LedgerId = id });
        return Ok(preview);
    }

    [HttpDelete("{id}/allocations/{allocationId}")]
    public async Task<ActionResult> DeleteAllocation([FromRoute] Guid id, [FromRoute] Guid allocationId)
    {
        await mediator.Send(new DeleteAllocationCommand(id, allocationId));
        return NoContent();
    }

    [HttpGet("{id}/allocationTemplates")]
    public async Task<ActionResult> GetAllocationTemplates([FromRoute] Guid id)
    {
        var templates = await mediator.Send(new GetLedgerAllocationTemplatesQuery(id));
        return Ok(templates);
    }

    [HttpPost("{id}/allocationTemplates")]
    public async Task<ActionResult> CreateAllocationTemplate([FromRoute] Guid id, [FromBody] CreateLedgerAllocationTemplateCommand request)
    {
        var templateId = await mediator.Send(request with { LedgerId = id });
        return CreatedAtAction(nameof(GetAllocationTemplates), new { id }, templateId);
    }

    [HttpPut("{id}/allocationTemplates/{templateId}")]
    public async Task<ActionResult> UpdateAllocationTemplate([FromRoute] Guid id, [FromRoute] Guid templateId, [FromBody] UpdateLedgerAllocationTemplateCommand request)
    {
        await mediator.Send(request with { LedgerId = id, TemplateId = templateId });
        return NoContent();
    }

    [HttpDelete("{id}/allocationTemplates/{templateId}")]
    public async Task<ActionResult> DeleteAllocationTemplate([FromRoute] Guid id, [FromRoute] Guid templateId)
    {
        await mediator.Send(new DeleteLedgerAllocationTemplateCommand(id, templateId));
        return NoContent();
    }

    [HttpGet("{id}/statistics")]
    public async Task<ActionResult> GetLedgerStatistics([FromRoute] Guid id, [FromQuery] LedgerStatisticsFilters filters)
    {
        var statistics = await mediator.Send(new GetLedgerStatisticsQuery(id, filters));
        return Ok(statistics);
    }
}
