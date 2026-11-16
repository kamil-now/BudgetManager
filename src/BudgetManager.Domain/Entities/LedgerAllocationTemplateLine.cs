using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Models;

namespace BudgetManager.Domain.Entities;

/// <summary>
/// One step of a ledger's allocation template: a fixed <see cref="Amount"/> or a <see cref="Percent"/> of what is left,
/// for a budget.
/// </summary>
public sealed class LedgerAllocationTemplateLine : Entity
{
    public required Guid LedgerAllocationTemplateId { get; set; }
    public required Guid BudgetId { get; set; }
    public required int Sequence { get; set; }
    public required AllocationType Type { get; set; }
    public decimal? Amount { get; set; }
    public decimal? Percent { get; set; }

    public LedgerAllocationTemplate LedgerAllocationTemplate { get; set; } = null!;
    public Budget Budget { get; set; } = null!;

    public static LedgerAllocationTemplateLine Create(Guid templateId, LedgerAllocationLine line) => line.Type switch
    {
        AllocationType.Fixed => new LedgerAllocationTemplateLine
        {
            LedgerAllocationTemplateId = templateId,
            BudgetId = line.BudgetId,
            Sequence = line.Sequence,
            Type = AllocationType.Fixed,
            Amount = line.Amount ?? throw new ArgumentNullException(nameof(line), "A fixed allocation line needs an amount.")
        },
        AllocationType.Percent => new LedgerAllocationTemplateLine
        {
            LedgerAllocationTemplateId = templateId,
            BudgetId = line.BudgetId,
            Sequence = line.Sequence,
            Type = AllocationType.Percent,
            Percent = line.Percent ?? throw new ArgumentNullException(nameof(line), "A percent allocation line needs a percent.")
        },
        _ => throw new NotSupportedException($"Unsupported allocation type '{line.Type}'.")
    };

    public AllocationRule ToRule() => new(BudgetId, Sequence, Type, (Type == AllocationType.Fixed ? Amount : Percent) ?? 0);
}
