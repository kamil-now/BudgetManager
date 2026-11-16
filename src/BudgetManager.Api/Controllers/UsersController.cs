using BudgetManager.Api.Configuration;
using BudgetManager.Application.Commands;
using BudgetManager.Application.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BudgetManager.Api.Controllers;

[Authorize]
public class UsersController(IMediator mediator) : BaseController
{
    [HttpGet("me/preferences")]
    public async Task<ActionResult> GetPreferences()
    {
        var preferences = await mediator.Send(new GetUserPreferencesQuery());
        return Ok(preferences);
    }

    [HttpPut("me/preferences")]
    public async Task<ActionResult> UpdatePreferences([FromBody] UpdateUserPreferencesCommand request)
    {
        await mediator.Send(request);
        return NoContent();
    }

    [HttpDelete("me")]
    [EnableRateLimiting(AuthRateLimit.PolicyName)]
    public async Task<ActionResult> DeleteMe([FromBody] DeleteUserCommand request)
    {
        await mediator.Send(request);
        return NoContent();
    }
}
