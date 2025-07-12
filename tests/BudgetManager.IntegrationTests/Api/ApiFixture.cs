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
  private WebApplicationFactory<Program>? Factory { get; set; }
  private HttpClient? Client { get; set; }

  protected override void AddServices(IServiceCollection services, IConfiguration? configuration)
  {
    // Configure test web application
    Factory = new WebApplicationFactory<Program>()
     .WithWebHostBuilder(builder =>
     {
       builder.ConfigureServices(services =>
       {
         // Remove existing DbContext registration
         var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
         if (descriptor != null)
           services.Remove(descriptor);

         // Add in-memory database
         services.AddDbContext<ApplicationDbContext>(options =>
           options
             .UseInMemoryDatabase($"BudgetManagerTestDb_{Guid.NewGuid()}")
             .UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()))
             .EnableSensitiveDataLogging());
       });

       // Configure test environment
       builder.UseEnvironment("Test");
     });

    Client = Factory.CreateClient();
    // Add services for DI testing
    services.AddSingleton(Factory);
    services.AddSingleton(Client);
  }

  protected override async ValueTask DisposeAsyncCore()
  {
    Client?.Dispose();
    if (Factory != null)
      await Factory.DisposeAsync();
  }

  protected override IEnumerable<TestAppSettings> GetTestAppSettings()
  {
    yield return new() { Filename = "appsettings.Test.json", IsOptional = false };
  }
}