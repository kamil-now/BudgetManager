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

    [HttpPost("{accountId}/transaction")]
    public async Task<ActionResult> CreateTransaction([FromRoute] Guid accountId, [FromBody] CreateAccountTransactionCommand request)
    {
        if (accountId != request.AccountId)
        {
            request = request with { AccountId = accountId };
        }
        var id = await mediator.Send(request);
        return CreatedAtAction(nameof(GetTransaction), new { accountId, transactionId = id }, id);
    }

    [HttpGet("{accountId}/transaction/{transactionId}")]
    public async Task<ActionResult> GetTransaction([FromRoute] Guid accountId, [FromRoute] Guid transactionId)
    {
        throw new NotImplementedException(); //  TODO
    }

    [HttpPut("{accountId}/transaction/{transactionId}")]
    public async Task<ActionResult> CreateIncome([FromRoute] Guid accountId, [FromRoute] Guid transactionId, [FromBody] UpdateAccountTransactionCommand request)
    {
        if (accountId != request.AccountId)
        {
            request = request with { AccountId = accountId };
        }
        if (transactionId != request.Id)
        {
            request = request with { Id = transactionId };
        }
        await mediator.Send(request);
        return Ok();
    }

    [HttpGet("{accountId}/transfer/{transferId}")]
    public async Task<ActionResult> GetTransfer([FromRoute] Guid accountId, [FromRoute] Guid transferId)
    {
        throw new NotImplementedException(); //  TODO
    }

    [HttpPost("{accountId}/transfer")]
    public async Task<ActionResult> CreateTransfer([FromRoute] Guid accountId, [FromBody] CreateAccountTransferCommand request)
    {
        if (accountId != request.AccountId)
        {
            request = request with { AccountId = accountId };
        }
        var id = await mediator.Send(request);
        return CreatedAtAction(nameof(GetTransfer), new { accountId, transferId = id }, id);
    }

    [HttpPost("{accountId}/currencyExchange")]
    public async Task<ActionResult> CreateCurrencyExchange([FromRoute] Guid accountId, [FromBody] CreateCurrencyExchangeCommand request)
    {
        if (accountId != request.AccountId)
        {
            request = request with { AccountId = accountId };
        }
        var id = await mediator.Send(request);
        return CreatedAtAction(nameof(GetTransfer), new { accountId, transferId = id }, id);
    }
}
