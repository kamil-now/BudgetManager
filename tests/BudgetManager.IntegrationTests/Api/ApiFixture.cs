using BudgetManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit.Microsoft.DependencyInjection;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace BudgetManager.IntegrationTests.Api;

public class ApiFixture : TestBedFixture
{
    private static readonly Lock FactoryLock = new();

    public static HttpClient? SharedClient { get; private set; }

    private WebApplicationFactory<Program>? Factory { get; set; }
    private HttpClient? Client { get; set; }

    public ApiFixture()
    {
        var connectionString = TestDatabase.CreateMigratedDatabase();

        lock (FactoryLock)
        {
            Factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                        services.RemoveAll<DbContextOptions>();
                        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
                    });

                    builder.UseEnvironment("Test");
                });

            Client = Factory.CreateClient();
        }

        SharedClient = Client;
    }

    protected override void AddServices(IServiceCollection services, IConfiguration? configuration)
    {
        services.AddSingleton(Factory!);
        services.AddSingleton(Client!);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        SharedClient = null;
        Client?.Dispose();
        if (Factory != null)
            await Factory.DisposeAsync();
    }

    protected override IEnumerable<TestAppSettings> GetTestAppSettings()
    {
        yield return new() { Filename = "appsettings.Test.json", IsOptional = false };
    }
}
