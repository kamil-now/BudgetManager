using BudgetManager.Application.Commands;
using BudgetManager.Application.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetManager.Api.Controllers;

[Authorize]
public class BudgetsController(IMediator mediator) : BaseController
{
    [HttpGet("{budgetId}")]
    public async Task<ActionResult> GetBudget([FromRoute] Guid budgetId)
    {
        var budget = await mediator.Send(new GetBudgetQuery(budgetId));
        return Ok(budget);
    }

    [HttpPost]
    public async Task<ActionResult> CreateBudget([FromBody] CreateBudgetCommand request)
    {
        var id = await mediator.Send(request);
        return CreatedAtAction(nameof(GetBudget), new { budgetId = id }, id);
    }

    [HttpPut("{budgetId}")]
    public async Task<ActionResult> UpdateBudget([FromRoute] Guid budgetId, [FromBody] UpdateBudgetCommand request)
    {
        await mediator.Send(request with { Id = budgetId });
        return NoContent();
    }

    [HttpGet("{budgetId}/allocationTemplates")]
    public async Task<ActionResult> GetAllocationTemplates([FromRoute] Guid budgetId)
    {
        var templates = await mediator.Send(new GetAllocationTemplatesQuery(budgetId));
        return Ok(templates);
    }

    [HttpPost("{budgetId}/allocationTemplates")]
    public async Task<ActionResult> CreateAllocationTemplate([FromRoute] Guid budgetId, [FromBody] CreateAllocationTemplateCommand request)
    {
        var id = await mediator.Send(request with { BudgetId = budgetId });
        return CreatedAtAction(nameof(GetAllocationTemplates), new { budgetId }, id);
    }

    [HttpPut("{budgetId}/allocationTemplates/{templateId}")]
    public async Task<ActionResult> UpdateAllocationTemplate([FromRoute] Guid budgetId, [FromRoute] Guid templateId, [FromBody] UpdateAllocationTemplateCommand request)
    {
        await mediator.Send(request with { BudgetId = budgetId, TemplateId = templateId });
        return NoContent();
    }

    [HttpDelete("{budgetId}/allocationTemplates/{templateId}")]
    public async Task<ActionResult> DeleteAllocationTemplate([FromRoute] Guid budgetId, [FromRoute] Guid templateId)
    {
        await mediator.Send(new DeleteAllocationTemplateCommand(budgetId, templateId));
        return NoContent();
    }
}
