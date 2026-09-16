using System.Linq.Expressions;
using BudgetManager.Application.Interfaces;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Security;

public abstract record Resource(Guid Id)
{
    public static Resource Of<T>(Guid id) where T : Entity, IAccessControlled<T> => new Resource<T>(id, T.OwnerId);

    public abstract Type EntityType { get; }

    public abstract Task<Guid?> ReadOwnerIdAsync(IResourceOwnerReader ownerReader, CancellationToken cancellationToken);
}

public sealed record Resource<T>(Guid Id, Expression<Func<T, Guid>> OwnerSelector) : Resource(Id) where T : Entity
{
    public override Type EntityType => typeof(T);

    public override Task<Guid?> ReadOwnerIdAsync(IResourceOwnerReader ownerReader, CancellationToken cancellationToken)
        => ownerReader.ReadOwnerIdAsync(Id, OwnerSelector, cancellationToken);
}
