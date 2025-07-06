using BudgetManager.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetManager.Infrastructure.Events;

public class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
  private readonly IServiceProvider _serviceProvider = serviceProvider;

  public async Task DispatchAsync<T>(T domainEvent, CancellationToken cancellationToken) where T : IDomainEvent
  {
    var handlers = _serviceProvider.GetServices<IDomainEventHandler<IDomainEvent>>()
      ?? throw new InvalidOperationException($"No handler registered for event type {typeof(T)}");

    foreach (var handler in handlers)
    {
      await handler.HandleAsync(domainEvent, cancellationToken);
    }
  }
}