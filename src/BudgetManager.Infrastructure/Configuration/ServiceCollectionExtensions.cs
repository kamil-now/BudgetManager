using System.Text;
using BudgetManager.Application.Interfaces;
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
using Microsoft.IdentityModel.Tokens;

namespace BudgetManager.Infrastructure.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection UseBudgetManagerAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        var jwtSection = configuration.GetSection("JwtTokenSettings");
        if (!jwtSection.Exists())
            throw new InvalidOperationException("JwtTokenSettings configuration section is missing");

        services.Configure<JwtTokenSettings>(jwtSection);

        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        services.AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", options =>
            {
                var jwtSettings = jwtSection.Get<JwtTokenSettings>()
                      ?? throw new InvalidOperationException("JwtTokenSettings configuration is invalid");
                if (jwtSettings.Secret == string.Empty)
                {
                    throw new InvalidOperationException("JwtTokenSettings.Secret cannot be empty");
                }
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                    ClockSkew = TimeSpan.Zero
                };
            });
        services.AddAuthorization();
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
