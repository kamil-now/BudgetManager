namespace BudgetManager.Domain.Interfaces;

public interface IDomainEventDispatcher
{
    Task DispatchAsync<T>(T domainEvent, CancellationToken cancellationToken) where T : IDomainEvent;
}
