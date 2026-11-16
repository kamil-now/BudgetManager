using System.Linq.Expressions;
using BudgetManager.Domain.Interfaces;
using BudgetManager.Domain.Models;
using BudgetManager.Domain.Services;

namespace BudgetManager.Domain.Entities;

/// <summary>
/// A named pipeline of allocation steps in one currency: the lines run in sequence order, each on what the lines
/// before it left. A budget can have several templates, also in the same currency, as long as their names differ.
/// The currency is fixed when the template is created.
/// </summary>
public class AllocationTemplate : Entity, IAccessControlled<AllocationTemplate>
{
    public required Guid BudgetId { get; set; }
    public required string Name { get; set; }
    public required string Currency { get; set; }

    public Budget Budget { get; set; } = null!;

    public virtual ICollection<AllocationTemplateLine> Lines { get; set; } = [];

    public static Expression<Func<AllocationTemplate, Guid>> OwnerId => x => x.Budget.Ledger.OwnerId;

    public static AllocationTemplate Create(Guid budgetId, string name, string currency, IEnumerable<AllocationLine> lines)
    {
        var template = new AllocationTemplate { BudgetId = budgetId, Name = name, Currency = currency };
        foreach (var line in lines)
        {
            template.Lines.Add(AllocationTemplateLine.Create(template.Id, line.FundId, line.Sequence, line.Type, line.Amount, line.Percent));
        }
        return template;
    }

    /// <summary>
    /// Replaces the template's lines with <paramref name="lines"/> wholesale: every existing line is removed and a new
    /// line is added for each of <paramref name="lines"/>, so a fund can have more than one line.
    /// </summary>
    /// <returns>
    /// The lines added. Their ids are set already, so a tracked template's new lines have to be added to the store
    /// explicitly; found only through <see cref="Lines"/> they would be taken for existing rows.
    /// </returns>
    public IReadOnlyList<AllocationTemplateLine> ReplaceLines(IEnumerable<AllocationLine> lines)
    {
        var added = lines
            .Select(x => AllocationTemplateLine.Create(Id, x.FundId, x.Sequence, x.Type, x.Amount, x.Percent))
            .ToArray();

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
    /// The smallest amount that funds every fixed line of the template fully.
    /// </summary>
    public decimal MinimumAmount() => AllocationCalculator.Minimum(ToRules());
}
