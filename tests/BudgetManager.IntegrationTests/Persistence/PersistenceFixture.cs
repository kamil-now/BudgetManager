using BudgetManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Microsoft.DependencyInjection;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace BudgetManager.IntegrationTests.Persistence;

public class PersistenceFixture : TestBedFixture
{
    protected override void AddServices(IServiceCollection services, IConfiguration? configuration)
    {
        var connectionString = TestDatabase.CreateMigratedDatabase();
        services.AddDbContext<ApplicationDbContext>(options =>
          options.UseNpgsql(connectionString).LogTo(Console.WriteLine, LogLevel.Warning));
    }

    protected override ValueTask DisposeAsyncCore()
        => new();

    protected override IEnumerable<TestAppSettings> GetTestAppSettings()
    {
        yield return new() { Filename = "appsettings.Test.json", IsOptional = false };
    }
}
