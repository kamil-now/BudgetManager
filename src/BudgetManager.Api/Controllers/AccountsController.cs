using BudgetManager.Application.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetManager.Api.Controllers;

[Authorize]
public class AccountsController(IMediator mediator) : BaseController
{
    [HttpGet("{accountId}")]
    public async Task<ActionResult> GetAccount([FromRoute] Guid accountId)
    {
        throw new NotImplementedException(); //  TODO
    }

    [HttpPost]
    public async Task<ActionResult> CreateAccount([FromBody] CreateAccountCommand request)
    {
        var id = await mediator.Send(request);
        return CreatedAtAction(nameof(GetAccount), new { accountId = id }, id);
    }

    [HttpGet("{accountId}/income/{incomeId}")]
    public async Task<ActionResult> GetIncome([FromRoute] Guid accountId, [FromRoute] Guid incomeId)
    {
        throw new NotImplementedException(); //  TODO
    }

    [HttpPost("{accountId}/income")]
    public async Task<ActionResult> CreateIncome([FromRoute] Guid accountId, [FromBody] CreateIncomeCommand request)
    {
        if (accountId != request.AccountId)
        {
            request = request with { AccountId = accountId };
        }
        var id = await mediator.Send(request);
        return CreatedAtAction(nameof(GetIncome), new { accountId, incomeId = id }, id);
    }

    [HttpPut("{accountId}/income/{incomeId}")]
    public async Task<ActionResult> CreateIncome([FromRoute] Guid accountId, [FromRoute] Guid incomeId, [FromBody] UpdateIncomeCommand request)
    {
        if (accountId != request.AccountId)
        {
            request = request with { AccountId = accountId };
        }
        if (incomeId != request.Id)
        {
            request = request with { Id = incomeId };
        }
        await mediator.Send(request);
        return Ok();
    }

    [HttpGet("{accountId}/expense/{expenseId}")]
    public async Task<ActionResult> GetExpense([FromRoute] Guid accountId, [FromRoute] Guid expenseId)
    {
        throw new NotImplementedException(); //  TODO
    }

    [HttpPost("{accountId}/expense")]
    public async Task<ActionResult> CreateExpense([FromRoute] Guid accountId, [FromBody] CreateExpenseCommand request)
    {
        if (accountId != request.AccountId)
        {
            request = request with { AccountId = accountId };
        }
        var id = await mediator.Send(request);
        return CreatedAtAction(nameof(GetExpense), new { accountId, expenseId = id }, id);
    }

    [HttpGet("{accountId}/transfer/{transferId}")]
    public async Task<ActionResult> GetTransfer([FromRoute] Guid accountId, [FromRoute] Guid transferId)
    {
        throw new NotImplementedException(); //  TODO
    }

    [HttpPost("{accountId}/transfer")]
    public async Task<ActionResult> CreateTransfer([FromRoute] Guid accountId, [FromBody] CreateTransferCommand request)
    {
        if (accountId != request.AccountId)
        {
            request = request with { AccountId = accountId };
        }
        var id = await mediator.Send(request);
        return CreatedAtAction(nameof(GetTransfer), new { accountId, transferId = id }, id);
    }
}
