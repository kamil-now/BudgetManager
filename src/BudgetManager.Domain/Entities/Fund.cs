using BudgetManager.Common.Enums;
using BudgetManager.Common.Models;

namespace BudgetManager.Domain.Entities;

public class Fund : Entity
{
    public required Guid BudgetId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int AllocationTemplateSequence { get; set; }
    public decimal AllocationTemplateValue { get; set; }
    public AllocationType AllocationTemplateType { get; set; }

    public Budget Budget { get; set; } = null!;

    public virtual ICollection<FundTransaction> Transactions { get; set; } = [];

    public Balance GetBalance()
    {
        Balance balance = [];
        foreach (var x in Transactions)
        {
            balance.Add(x.Value);
        }
        foreach (var key in balance.Where(x => x.Value == 0).Select(x => x.Key).ToList())
        {
            balance.Remove(key);
        }
        return balance;
    }
}
