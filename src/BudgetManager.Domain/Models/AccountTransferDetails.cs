namespace BudgetManager.Domain.Models;

public record AccountTransferDetails(
    Guid Id,
    Guid ExpenseAccountId,
    string ExpenseAccountName,
    Money ExpenseValue,
    Guid IncomeAccountId,
    string IncomeAccountName,
    Money IncomeValue,
    DateTimeOffset Date,
    string? Title,
    string? Comment,
    List<string>? Tags);
