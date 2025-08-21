using BudgetManager.Application.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetManager.Api.Controllers;

[Authorize]
public class AccountsController(IMediator mediator) : BaseController
{
    [HttpPost]
    public async Task<ActionResult> CreateAccount([FromBody] CreateAccountCommand request)
    {
        var id = await mediator.Send(request);
        return Ok(id);
    }
}
