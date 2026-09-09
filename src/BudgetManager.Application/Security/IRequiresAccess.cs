namespace BudgetManager.Application.Security;

public interface IRequiresAccess
{
    IEnumerable<Resource> Resources { get; }
}
