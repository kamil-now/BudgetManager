
using BudgetManager.Api.Configuration;
using BudgetManager.Api.Models;
using BudgetManager.Application.Commands;
using BudgetManager.Infrastructure.Auth.Interfaces;
using BudgetManager.Infrastructure.Auth.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BudgetManager.Api.Controllers;

[AllowAnonymous]
[EnableRateLimiting(AuthRateLimit.PolicyName)]
public class AuthController(IMediator mediator, IJwtTokenGenerator jwtGenerator) : BaseController
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var user = await mediator.Send(command);
        var token = jwtGenerator.GenerateToken(new UserDTO(user.Id, user.Name, user.Email));

        return Ok(new TokenResponse(token));
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] CreateUserCommand command)
    {
        var user = await mediator.Send(command);
        return Ok(user.Id);
    }
}
