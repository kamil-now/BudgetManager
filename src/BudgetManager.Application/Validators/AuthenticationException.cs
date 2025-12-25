namespace BudgetManager.Application.Validators;

public class AuthenticationException(string? message = "User not authenticated.") : Exception(message);
