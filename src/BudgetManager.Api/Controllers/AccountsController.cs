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

    [HttpPost("{accountId}/income")]
    public async Task<ActionResult> CreateIncome([FromRoute] Guid accountId, [FromBody] CreateIncomeCommand request)
    {
        if (accountId != request.AccountId)
        {
            request = request with { AccountId = accountId };
        }
        var id = await mediator.Send(request);
        return Ok(id);
    }

    [HttpPost("{accountId}/expense")]
    public async Task<ActionResult> CreateIncome([FromRoute] Guid accountId, [FromBody] CreateExpenseCommand request)
    {
        if (accountId != request.AccountId)
        {
            request = request with { AccountId = accountId };
        }
        var id = await mediator.Send(request);
        return Ok(id);
    }

    [HttpPost("{accountId}/transfer")]
    public async Task<ActionResult> CreateIncome([FromRoute] Guid accountId, [FromBody] CreateTransferCommand request)
    {
        if (accountId != request.AccountId)
        {
            request = request with { AccountId = accountId };
        }
        var id = await mediator.Send(request);
        return Ok(id);
    }
}
