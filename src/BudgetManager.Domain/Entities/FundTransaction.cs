using BudgetManager.Common.Models;

namespace BudgetManager.Domain.Entities;

public class FundTransaction : Entity
{
    public required Guid FundId { get; set; }
    public required Money Value { get; set; }
    public DateTimeOffset Date { get; set; }
    public string? Title { get; set; }
    public string? Comment { get; set; }

    public FundTransfer? InTransfer { get; set; }
    public FundTransfer? OutTransfer { get; set; }
    public Fund Fund { get; set; } = null!;
}
