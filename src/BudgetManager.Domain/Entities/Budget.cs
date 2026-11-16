using System.Linq.Expressions;
using BudgetManager.Domain.Interfaces;
using BudgetManager.Domain.Models;

namespace BudgetManager.Domain.Entities;

public class Budget : Entity, IAccessControlled<Budget>
{
    public required Guid LedgerId { get; set; }
    public required string Name { get; set; }

    public Ledger Ledger { get; set; } = null!;
    public virtual ICollection<Fund> Funds { get; set; } = [];
    public virtual ICollection<AllocationTemplate> AllocationTemplates { get; set; } = [];

    public static Expression<Func<Budget, Guid>> OwnerId => x => x.Ledger.OwnerId;

    public Fund AddFund(string name)
    {
        var fund = new Fund
        {
            BudgetId = Id,
            Name = name
        };

        Funds.Add(fund);

        return fund;
    }

    public AllocationTemplate AddAllocationTemplate(string name, string currency, IEnumerable<AllocationLine> lines)
    {
        var template = AllocationTemplate.Create(Id, name, currency, lines);

        AllocationTemplates.Add(template);

        return template;
    }
}
