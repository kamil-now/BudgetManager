using BudgetManager.Domain.Interfaces;
using BudgetManager.Infrastructure.Auth.Interfaces;
using BudgetManager.Infrastructure.Auth.Models;
using BudgetManager.Infrastructure.Auth.Services;
using BudgetManager.Infrastructure.Events;
using BudgetManager.Infrastructure.Persistence;
using BudgetManager.Infrastructure.Persistence.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetManager.Infrastructure.Configuration;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection UseBudgetManagerAuth(this IServiceCollection services, IConfiguration configuration)
  {
    services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
    services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

    services.Configure<JwtTokenSettings>(options => configuration.GetSection("JWT").Bind(options));

    return services;
  }

  public static IServiceCollection UsePostgreSQL(this IServiceCollection services, IConfiguration configuration)
  {
    services.AddScoped<IBudgetManagerService, BudgetManagerService>();

    var connectionString = configuration.GetConnectionString("DefaultConnection");
    services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
    return services;
  }

  public static IServiceCollection UseDomainEvents<T>(this IServiceCollection services)
  {
    services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
    
    // Register all handlers from the assembly of the provided type
    var assembly = typeof(T).Assembly;
    // Find all types in the assembly that implement IDomainEventHandler<,> or IRequestHandler<>
    var handlerTypes = assembly.GetTypes()
      .Where(t => !t.IsAbstract && !t.IsInterface)
      .Where(t =>
        t.GetInterfaces().Any(i =>
          i.IsGenericType &&
          (
            i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>)
          )
        )
      );

    foreach (var handlerType in handlerTypes)
    {
      foreach (var handlerInterface in handlerType.GetInterfaces()
        .Where(i => i.IsGenericType &&
          (
            i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>)
          )
        ))
      {
        services.AddScoped(handlerInterface, handlerType);
      }
    }

    return services;
  }
}
