using BudgetManager.DemoDataInitializer;
using BudgetManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetManager.IntegrationTests.Seeding;

using DemoDataInitializer = BudgetManager.DemoDataInitializer.DemoDataInitializer;

public class DemoDataFixture : IAsyncLifetime
{
    public ServiceProvider Services { get; private set; } = null!;

    public bool FirstRunInitialized { get; private set; }

    public async Task InitializeAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = TestDatabase.CreateMigratedDatabase() })
            .Build();
        Services = InitializerServices.Create(configuration);

        await using var scope = Services.CreateAsyncScope();
        FirstRunInitialized = await scope.ServiceProvider.GetRequiredService<DemoDataInitializer>().InitializeAsync();
    }

    public async Task DisposeAsync() => await Services.DisposeAsync();

    public async Task<AsyncServiceScope> CreateSignedInScopeAsync()
    {
        var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userId = await dbContext.Users.Where(x => x.Email == DemoDataInitializer.Email).Select(x => x.Id).SingleAsync();
        scope.ServiceProvider.GetRequiredService<DemoUser>().SignIn(userId);
        return scope;
    }
}
