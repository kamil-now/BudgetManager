using System.Linq.Expressions;
using BudgetManager.Domain.Entities;

namespace BudgetManager.Domain.Interfaces;

public interface IAccessControlled<TSelf> where TSelf : Entity
{
    static abstract Expression<Func<TSelf, Guid>> OwnerId { get; }
}
