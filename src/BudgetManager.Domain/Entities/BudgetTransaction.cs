using BudgetManager.Common.Models;

namespace BudgetManager.Domain.Entities;

public abstract class BudgetTransaction : Entity
{
    public required Guid FundId { get; set; }
    public required Money Amount { get; set; }
    public DateTimeOffset Date { get; set; }
    public string? Title { get; set; }
    public string? Comment { get; set; }

    public Fund Fund { get; set; } = null!;
}
