using BudgetManager.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace BudgetManager.IntegrationTests.Persistence;

public abstract class BaseTest(ITestOutputHelper testOutputHelper, PersistenceFixture fixture) : TestBed<PersistenceFixture>(testOutputHelper, fixture)
{
  public ApplicationDbContext GetContext()
  {
    var scope = _fixture.GetServiceProvider(_testOutputHelper).CreateScope();
    var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>()
      ?? throw new InvalidOperationException($"{nameof(ApplicationDbContext)} is not registered in the service collection.");

    dbContext.Database.EnsureCreated();

    return dbContext;
  }
}
