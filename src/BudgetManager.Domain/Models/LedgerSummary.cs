namespace BudgetManager.Domain.Models;

public record LedgerSummary
{
  public required Guid Id { get; init; }
  public required string Name { get; init; }
  public string? Description { get; init; }
  public required IEnumerable<Account> Accounts { get; init; }
  public required IEnumerable<Budget> Budgets { get; init; }

  public record Account(Guid Id, string Name, string? Description, Balance Balance);

  public record Budget(Guid Id, string Name, string? Description, IEnumerable<Fund> Funds);

  public record Fund(Guid Id, string Name, string? Description, Balance Balance);
}
