using BudgetManager.Application.Models;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Handlers;

internal static class BudgetExtensions
{
    /// <summary>
    /// Adds the validated funds to a new budget, then its allocation templates, whose lines name funds by name.
    /// </summary>
    public static void AddFundsAndTemplates(
        this Budget budget,
        IEnumerable<CreateFundDTO> funds,
        IEnumerable<CreateAllocationTemplateDTO>? templates)
    {
        var added = funds.ToDictionary(x => x.Name, x => budget.AddFund(x.Name));

        foreach (var template in templates ?? [])
        {
            budget.AddAllocationTemplate(
                template.Name,
                template.Currency,
                template.Lines.Select(x => new AllocationLine(added[x.Fund].Id, x.Sequence, x.Type, x.Amount, x.Percent)));
        }
    }
}
