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
    services.AddSingleton<ICurrentUserService, MockCurrentUserService>();
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
  public const string MockUserId = "4ec3a3b3-a6f5-4720-9c57-ba0495dec583"; 
  public const string MockUserEmail = "email@mock.com";
  public const string MockUserName = "Mock User"; 
  public string? Id => MockUserId;
  public string? Email => MockUserEmail;
  public string? Name => MockUserName;
}
