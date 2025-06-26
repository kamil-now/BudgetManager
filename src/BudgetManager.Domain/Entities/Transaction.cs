using BudgetManager.Common.Models;

namespace BudgetManager.Domain.Entities;

public abstract class Transaction : Entity
{
  public required Guid LedgerId { get; init; }
  public required Guid AccountId { get; init; }
  public required Money Amount { get; init; }
  public DateTimeOffset Date { get; init; }
  public required string Title { get; init; }
  public string? Description { get; init; }
  public List<string>? Tags { get; init; }
}
