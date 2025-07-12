using BudgetManager.Application.Configuration;
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

namespace BudgetManager.IntegrationTests.Fixtures;

public class ApplicationFixture : TestBedFixture
{
  protected override void AddServices(IServiceCollection services, IConfiguration? configuration)
  {
    services.AddDbContext<ApplicationDbContext>(options =>
      options
        .UseInMemoryDatabase("BudgetManagerTestDb")
        .UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()))
        .EnableSensitiveDataLogging());

    services.AddScoped<IBudgetManagerService, BudgetManagerService>();
    services.UseMediator();
  }

  protected override ValueTask DisposeAsyncCore()
      => new();

  protected override IEnumerable<TestAppSettings> GetTestAppSettings()
  {
    yield return new() { Filename = "appsettings.Test.json", IsOptional = false };
  }
}
