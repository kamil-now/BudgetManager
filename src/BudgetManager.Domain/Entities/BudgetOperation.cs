using BudgetManager.Common.Models;

namespace BudgetManager.Domain.Entities;

public abstract class BudgetOperation : Entity
{
  public required Guid BudgetId { get; init; }
  public required Guid FundId { get; init; }
  public required Money Amount { get; init; }
  public DateTimeOffset Date { get; init; }
  public string? Description { get; init; }
}
