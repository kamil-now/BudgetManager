namespace BudgetManager.Domain.Entities;

public class Fund : Entity
{
  public required Guid BudgetId { get; init; }
  public required string Name { get; init; }
}
