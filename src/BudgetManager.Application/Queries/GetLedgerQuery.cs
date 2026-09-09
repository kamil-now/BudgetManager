using BudgetManager.Application.Models;
using BudgetManager.Application.Security;
using BudgetManager.Domain.Entities;

namespace BudgetManager.Application.Queries;

public record GetLedgerQuery(Guid Id) : IRequest<LedgerDTO?>, IRequiresAccess
{
  IEnumerable<Resource> IRequiresAccess.Resources => [Resource.Of<Ledger>(Id)];
}
