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
    private IServiceProvider? _serviceProvider;

    protected override void AddServices(IServiceCollection services, IConfiguration? configuration)
    {
        var connectionString = configuration!.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationDbContext>(options =>
          options.UseNpgsql(connectionString).LogTo(Console.WriteLine, LogLevel.Warning));

        _serviceProvider = services.BuildServiceProvider();
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        if (_serviceProvider != null)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.EnsureDeletedAsync();
        }
    }

    protected override IEnumerable<TestAppSettings> GetTestAppSettings()
    {
        yield return new() { Filename = "appsettings.Test.json", IsOptional = false };
    }
}
