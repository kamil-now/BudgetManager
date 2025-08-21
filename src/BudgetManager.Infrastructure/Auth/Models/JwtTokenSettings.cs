namespace BudgetManager.Infrastructure.Auth.Models;

public sealed record JwtTokenSettings(string Secret, string Issuer, string Audience, int ExpirationMinutes)
{
    public JwtTokenSettings() : this(string.Empty, string.Empty, string.Empty, 0) { }
}
