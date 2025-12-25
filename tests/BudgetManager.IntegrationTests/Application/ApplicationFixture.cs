using BudgetManager.Application.Configuration;
using BudgetManager.Application.Services;
using BudgetManager.Common;
using BudgetManager.Domain.Interfaces;
using BudgetManager.Infrastructure.Persistence;
using BudgetManager.Infrastructure.Persistence.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Microsoft.DependencyInjection;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace BudgetManager.IntegrationTests.Application;

public class ApplicationFixture : TestBedFixture
{
    protected override void AddServices(IServiceCollection services, IConfiguration? configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
          options
            .UseInMemoryDatabase("BudgetManagerTestDb")
            .UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()))
            .EnableSensitiveDataLogging());

        services.AddSingleton<IBudgetManagerService, BudgetManagerService>();
        services.AddSingleton<MockCurrentUserService>();
        services.AddSingleton<ICurrentUserService>(s => s.GetService<MockCurrentUserService>()!);

        services.UseMediator();
    }

    protected override ValueTask DisposeAsyncCore()
        => new();

    protected override IEnumerable<TestAppSettings> GetTestAppSettings()
    {
        yield return new() { Filename = "appsettings.Test.json", IsOptional = false };
    }
}

public class MockCurrentUserService : ICurrentUserService
{
    public string? MockUserId { get; set; }
    public string? MockUserEmail { get; set; }
    public string? MockUserName { get; set; }
    public string? Id => MockUserId;
    public string? Email => MockUserEmail;
    public string? Name => MockUserName;
}
