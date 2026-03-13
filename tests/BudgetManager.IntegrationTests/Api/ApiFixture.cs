using BudgetManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Microsoft.DependencyInjection;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace BudgetManager.IntegrationTests.Api;

public class ApiFixture : TestBedFixture
{
    public static HttpClient? SharedClient { get; private set; }

    private WebApplicationFactory<Program>? Factory { get; set; }
    private HttpClient? Client { get; set; }

    public ApiFixture()
    {
        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext registration
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase($"BudgetManagerTestDb_{Guid.NewGuid()}")
                    .UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()))
                    .EnableSensitiveDataLogging()
                    .Options;

                    var sharedDbContext = new ApplicationDbContext(options);
                    services.AddSingleton(sharedDbContext);
                });

                builder.UseEnvironment("Test");
            });

        Client = Factory.CreateClient();
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