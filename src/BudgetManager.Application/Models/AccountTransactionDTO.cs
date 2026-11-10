using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Models;

/// <summary>
/// A single account transaction as the UI consumes it. A plain income/expense has no
/// <see cref="Received"/> or target; a transfer carries the target account; a currency exchange
/// carries both the received money and the target account.
/// </summary>
public record AccountTransactionDTO(
    Guid Id,
    Guid AccountId,
    string AccountName,
    Money Value,
    Money? Received,
    DateTimeOffset Date,
    Guid? TargetAccountId,
    string? TargetAccountName,
    string? Title,
    string? Comment,
    IEnumerable<string>? Tags);
