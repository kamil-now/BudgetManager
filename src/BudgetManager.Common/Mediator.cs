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
        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));

        var handler = serviceProvider.GetService(handlerType)
            ?? throw new InvalidOperationException($"No handler registered for request type {requestType}");

        var handleMethod = handlerType.GetMethod("Handle")
          ?? throw new InvalidOperationException($"Handler {handlerType} does not implement Handle method.");

        var task = handleMethod.Invoke(handler, [request, cancellationToken]) as Task<TResponse>
          ?? throw new InvalidOperationException($"Handler {handlerType} did not return a valid task.");

        return await task;
    }
}
