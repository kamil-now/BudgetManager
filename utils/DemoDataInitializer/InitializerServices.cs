using BudgetManager.Application.Configuration;
using BudgetManager.Application.Interfaces;
using BudgetManager.Application.Services;
using BudgetManager.Infrastructure.Auth.Services;
using BudgetManager.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetManager.DemoDataInitializer;

public static class InitializerServices
{
    /// <summary>
    /// The services the initializer needs: the database, the mediator with all its handlers and a current user the initializer signs in itself.
    /// The authentication setup of the API is left out, so no JWT settings are needed.
    /// </summary>
    public static ServiceProvider Create(IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("DefaultConnection")))
        {
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");
        }

        var services = new ServiceCollection();
        services.UsePostgreSQL(configuration);
        services.UseMediator();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<DemoUser>();
        services.AddScoped<ICurrentUserService>(s => s.GetRequiredService<DemoUser>());
        services.AddScoped<DemoDataInitializer>();
        return services.BuildServiceProvider();
    }
}
