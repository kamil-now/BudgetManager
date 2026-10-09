using System.Linq.Expressions;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Interfaces;
using BudgetManager.Domain.Models;

namespace BudgetManager.Domain.Entities;

public class Budget : Entity, IAccessControlled<Budget>
{
    public required Guid LedgerId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }

    public Ledger Ledger { get; set; } = null!;
    public virtual ICollection<Fund> Funds { get; set; } = [];
    public virtual ICollection<AllocationTemplate> AllocationTemplates { get; set; } = [];

    public static Expression<Func<Budget, Guid>> OwnerId => x => x.Ledger.OwnerId;

    public void AddFund(string name, string? description, IEnumerable<FundAllocation> allocations)
    {
        var fund = new Fund
        {
            BudgetId = Id,
            Name = name,
            Description = description
        };

        foreach (var allocation in allocations)
        {
            var template = AllocationTemplates
                .SingleOrDefault(x => x.Currency == allocation.Currency);
            if (template == null)
            {
                template = new AllocationTemplate { BudgetId = Id, Currency = allocation.Currency };
                AllocationTemplates.Add(template);
            }

            template.Lines.Add(CreateAllocationTemplateLine(allocation, template.Id, fund.Id));
        }

        Funds.Add(fund);
    }

    private static AllocationTemplateLine CreateAllocationTemplateLine(FundAllocation allocation, Guid allocationTemplateId, Guid fundId) => allocation.Type switch
    {
        AllocationType.Fixed => new FixedAllocationTemplateLine
        {
            AllocationTemplateId = allocationTemplateId,
            FundId = fundId,
            Sequence = allocation.Sequence,
            Amount = allocation.Amount!.Value
        },
        AllocationType.Percent => new PercentAllocationTemplateLine
        {
            AllocationTemplateId = allocationTemplateId,
            FundId = fundId,
            Sequence = allocation.Sequence,
            Percent = allocation.Percent!.Value
        },
        _ => throw new NotSupportedException($"Unsupported allocation type '{allocation.Type}'.")
    };
}
