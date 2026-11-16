using System.Linq.Expressions;
using BudgetManager.Domain.Interfaces;
using BudgetManager.Domain.Models;
using BudgetManager.Domain.Services;

namespace BudgetManager.Domain.Entities;

/// <summary>
/// A named pipeline that splits an income between the ledger's budgets, in one currency, by the same rules as a budget's
/// <see cref="AllocationTemplate"/>. Each budget's share is then split between its funds by one of the budget's
/// templates, which is picked when the income is allocated. The currency is fixed when the template is created.
/// </summary>
public class LedgerAllocationTemplate : Entity, IAccessControlled<LedgerAllocationTemplate>
{
    public required Guid LedgerId { get; set; }
    public required string Name { get; set; }
    public required string Currency { get; set; }

    public Ledger Ledger { get; set; } = null!;

    public virtual ICollection<LedgerAllocationTemplateLine> Lines { get; set; } = [];

    public static Expression<Func<LedgerAllocationTemplate, Guid>> OwnerId => x => x.Ledger.OwnerId;

    public static LedgerAllocationTemplate Create(Guid ledgerId, string name, string currency, IEnumerable<LedgerAllocationLine> lines)
    {
        var template = new LedgerAllocationTemplate { LedgerId = ledgerId, Name = name, Currency = currency };
        foreach (var line in lines)
        {
            template.Lines.Add(LedgerAllocationTemplateLine.Create(template.Id, line));
        }
        return template;
    }

    /// <summary>
    /// Replaces the template's lines with <paramref name="lines"/> wholesale.
    /// </summary>
    /// <returns>
    /// The lines added. Their ids are set already, so a tracked template's new lines have to be added to the store
    /// explicitly; found only through <see cref="Lines"/> they would be taken for existing rows.
    /// </returns>
    public IReadOnlyList<LedgerAllocationTemplateLine> ReplaceLines(IEnumerable<LedgerAllocationLine> lines)
    {
        var added = lines.Select(x => LedgerAllocationTemplateLine.Create(Id, x)).ToArray();

        Lines.Clear();
        foreach (var line in added)
        {
            Lines.Add(line);
        }

        UpdatedAt = DateTimeOffset.UtcNow;

        return added;
    }

    public IReadOnlyList<AllocationRule> ToRules() => [.. Lines.Select(x => x.ToRule()).OrderBy(x => x.Sequence)];

    /// <summary>
    /// The smallest amount that funds every fixed line of the template fully. The minimums of the budget templates
    /// picked for its budgets come on top, when the income is allocated.
    /// </summary>
    public decimal MinimumAmount() => AllocationCalculator.Minimum(ToRules());
}
