namespace BudgetManager.Domain.Entities;

public abstract class Entity : IEquatable<Entity>
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public bool Equals(Entity? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return Id.Equals(other.Id);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not null && obj is Entity entity)
            return Equals(entity);
        return false;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}
