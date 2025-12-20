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

    [HttpGet("{id}")]
    public async Task<ActionResult> GetLedger([FromRoute] Guid id)
    {
        var ledger = await mediator.Send(new GetLedgerQuery(id));
        return Ok(ledger);
    }
}
