using Microsoft.Extensions.DependencyInjection;

namespace BudgetManager.Application.Configuration;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection UseMediator(this IServiceCollection services)
  {
    services.AddScoped<IMediator, Mediator>();
    var assembly = typeof(IAssemblyMarker).Assembly;
    // Find all types in the assembly that implement IRequestHandler<,> or IRequestHandler<>
    var handlerTypes = assembly.GetTypes()
      .Where(t => !t.IsAbstract && !t.IsInterface)
      .Where(t =>
        t.GetInterfaces().Any(i =>
          i.IsGenericType &&
          (
            i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)
          )
        )
      );

    foreach (var handlerType in handlerTypes)
    {
      foreach (var handlerInterface in handlerType.GetInterfaces()
        .Where(i => i.IsGenericType &&
          (
            i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)
          )
        ))
      {
        services.AddScoped(handlerInterface, handlerType);
      }
    }

    return services;
  }
}
