using Microsoft.Extensions.DependencyInjection;

namespace BudgetManager.Common;

public interface IMediator
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}

public interface IRequest<TResponse> { }

public interface IRequestHandler<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
  Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

public class Mediator(IServiceProvider serviceProvider) : IMediator
{
  private readonly IServiceProvider serviceProvider = serviceProvider;

  public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
  {
    var handler = serviceProvider.GetService<IRequestHandler<IRequest<TResponse>, TResponse>>()
      ?? throw new InvalidOperationException($"No handler registered for request type {typeof(IRequest<TResponse>)}");
    return await handler.Handle(request, cancellationToken);
  }
}