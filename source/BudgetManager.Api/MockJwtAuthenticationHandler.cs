namespace BudgetManager.Api;

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

public class MockJwtAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
    System.Text.Encodings.Web.UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "MockJwt";

    private readonly string _userId = "mock_user_id";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim>
        {
            new (ClaimTypes.NameIdentifier, _userId),
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
