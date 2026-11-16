using BudgetManager.Application.Models;
using BudgetManager.Application.Security;
using BudgetManager.Domain.Entities;

namespace BudgetManager.Application.Queries;

public record GetAllocationTemplatesQuery(Guid BudgetId) : IRequest<IEnumerable<AllocationTemplateDTO>>, IRequiresAccess
{
    IEnumerable<Resource> IRequiresAccess.Resources => [Resource.Of<Budget>(BudgetId)];
}
