using BudgetManager.Application.Commands;
using BudgetManager.Application.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetManager.Api.Controllers;

[Authorize]
public class AccountsController(IMediator mediator) : BaseController
{
    [HttpGet("{accountId}")]
    public async Task<ActionResult> GetAccount([FromRoute] Guid accountId)
    {
        var account = await mediator.Send(new GetAccountQuery(accountId));
        return Ok(account);
    }

    [HttpPost]
    public async Task<ActionResult> CreateAccount([FromBody] CreateAccountCommand request)
    {
        var id = await mediator.Send(request);
        return CreatedAtAction(nameof(GetAccount), new { accountId = id }, id);
    }

    [HttpPut("{accountId}")]
    public async Task<ActionResult> UpdateAccount([FromRoute] Guid accountId, [FromBody] UpdateAccountCommand request)
    {
        await mediator.Send(request with { Id = accountId });
        return NoContent();
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
        var transaction = await mediator.Send(new GetAccountTransactionQuery(accountId, transactionId));
        return Ok(transaction);
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

    [HttpDelete("{accountId}/transaction/{transactionId}")]
    public async Task<ActionResult> DeleteTransaction([FromRoute] Guid accountId, [FromRoute] Guid transactionId)
    {
        await mediator.Send(new DeleteAccountTransactionCommand(accountId, transactionId));
        return NoContent();
    }

    [HttpGet("{accountId}/transfer/{transferId}")]
    public async Task<ActionResult> GetTransfer([FromRoute] Guid accountId, [FromRoute] Guid transferId)
    {
        var transfer = await mediator.Send(new GetAccountTransferQuery(accountId, transferId));
        return Ok(transfer);
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

    [HttpPut("{accountId}/transfer/{transferId}")]
    public async Task<ActionResult> UpdateTransfer([FromRoute] Guid accountId, [FromRoute] Guid transferId, [FromBody] UpdateAccountTransferCommand request)
    {
        await mediator.Send(request with { Id = transferId, AccountId = accountId });
        return Ok();
    }

    [HttpDelete("{accountId}/transfer/{transferId}")]
    public async Task<ActionResult> DeleteTransfer([FromRoute] Guid accountId, [FromRoute] Guid transferId)
    {
        await mediator.Send(new DeleteAccountTransferCommand(accountId, transferId));
        return NoContent();
    }

    [HttpPut("{accountId}/currencyExchange/{transferId}")]
    public async Task<ActionResult> UpdateCurrencyExchange([FromRoute] Guid accountId, [FromRoute] Guid transferId, [FromBody] UpdateCurrencyExchangeCommand request)
    {
        await mediator.Send(request with { Id = transferId, AccountId = accountId });
        return Ok();
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
