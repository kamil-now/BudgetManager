using BudgetManager.Application.Commands;
using BudgetManager.Application.Queries;
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
    public async Task<ActionResult> GetLedger()
    {
        var id = await mediator.Send(new GetLedgerQuery());
        return Ok(id);
    }
}
