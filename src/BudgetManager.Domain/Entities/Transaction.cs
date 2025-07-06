using BudgetManager.Common.Models;

namespace BudgetManager.Domain.Entities;

public abstract class Transaction : Entity
{
  public required Guid AccountId { get; set; }
  public required Money Amount { get; set; }
  public DateTimeOffset Date { get; set; }
  public required string Title { get; set; }
  public string? Description { get; set; }
  public List<string>? Tags { get; set; }

  public Account Account { get; set; } = null!;
}
