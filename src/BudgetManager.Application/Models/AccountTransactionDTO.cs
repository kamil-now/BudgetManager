using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Models;

/// <summary>
/// A single account transaction as the UI consumes it. A plain income/expense has no
/// <see cref="Received"/> or target; a transfer carries the target account; a currency exchange
/// carries both the received money and the target account. <see cref="Allocations"/> lists the fund
/// allocations made from an income; it is empty for everything else. <see cref="FundId"/> is the fund an expense is paid from.
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
    IEnumerable<FundAmount> Allocations,
    Guid? FundId = null);
