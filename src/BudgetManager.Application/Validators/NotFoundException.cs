namespace BudgetManager.Application.Validators;

public class NotFoundException(string message) : Exception(message);
