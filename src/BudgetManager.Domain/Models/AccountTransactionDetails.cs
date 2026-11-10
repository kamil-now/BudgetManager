namespace BudgetManager.Domain.Models;

public record AccountTransactionDetails(
    Guid Id,
    Guid AccountId,
    string AccountName,
    Money Value,
    DateTimeOffset Date,
    string? Title,
    string? Comment,
    List<string>? Tags);
