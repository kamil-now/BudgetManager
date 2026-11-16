namespace BudgetManager.Application.Models;

/// <summary>
/// How an amount would be split: one line per allocation rule that gets money (a fund can appear more than once), and what is left.
/// </summary>
public record AllocationPreviewDTO(IEnumerable<AllocationPreviewDTO.Line> Lines, decimal Unallocated)
{
    public record Line(Guid FundId, string FundName, string BudgetName, decimal Amount);
}
