using System.Linq.Expressions;
using BudgetManager.Domain.Entities;

namespace BudgetManager.Application.Interfaces;

public interface IResourceOwnerReader
{
    Task<Guid?> ReadOwnerIdAsync<T>(Guid id, Expression<Func<T, Guid>> ownerSelector, CancellationToken cancellationToken = default) where T : Entity;
}
