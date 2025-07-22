using BudgetManager.Application.Commands;
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
}
