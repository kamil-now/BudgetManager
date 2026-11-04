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
    private readonly string _connectionString;

    public ApiFixture()
    {
        var connectionString = _connectionString = TestDatabase.CreateMigratedDatabase();

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
                    // every test logs in from the same address; RateLimitTests lowers it again
                    builder.UseSetting("AuthRateLimit:PermitLimit", int.MaxValue.ToString());
                });

            Client = Factory.CreateClient();
        }

        SharedClient = Client;
    }

    /// <summary>A client for the same app with extra host settings; built under the same lock as the main host.</summary>
    public HttpClient CreateClient(Action<IWebHostBuilder> configure)
    {
        lock (FactoryLock)
        {
            return Factory!.WithWebHostBuilder(configure).CreateClient();
        }
    }

    protected override void AddServices(IServiceCollection services, IConfiguration? configuration)
    {
        services.AddSingleton(Factory!);
        services.AddSingleton(Client!);
        // lets tests assert on what the API persisted, in the same database the API uses
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(_connectionString));
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
