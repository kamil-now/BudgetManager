using BudgetManager.Application.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetManager.Api.Controllers;

[Authorize]
public class BudgetsController(IMediator mediator) : BaseController
{
    [HttpGet("{budgetId}")]
    public async Task<ActionResult> GetBudget([FromRoute] Guid budgetId)
    {
        throw new NotImplementedException(); //  TODO
    }

    [HttpPost]
    public async Task<ActionResult> CreateBudget([FromBody] CreateBudgetCommand request)
    {
        var id = await mediator.Send(request);
        return CreatedAtAction(nameof(GetBudget), new { budgetId = id }, id);
    }
}
