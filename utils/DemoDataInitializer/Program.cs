using BudgetManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetManager.DemoDataInitializer;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        await using var provider = InitializerServices.Create(configuration);
        await using var scope = provider.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();

        var initialized = await scope.ServiceProvider.GetRequiredService<DemoDataInitializer>().InitializeAsync();
        Console.WriteLine(initialized
            ? $"Created demo data for {DemoDataInitializer.Email} / {DemoDataInitializer.Password}."
            : $"{DemoDataInitializer.Email} exists already, nothing created.");

        return 0;
    }
}
