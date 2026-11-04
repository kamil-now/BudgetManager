namespace BudgetManager.Api.Configuration;

public class AuthRateLimit
{
    public const string SectionName = "AuthRateLimit";
    public const string PolicyName = "auth";

    public int PermitLimit { get; set; } = 10;
    public int WindowSeconds { get; set; } = 60;
}
