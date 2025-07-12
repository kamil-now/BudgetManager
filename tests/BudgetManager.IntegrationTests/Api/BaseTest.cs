using BudgetManager.Infrastructure.Auth.Interfaces;
using BudgetManager.Infrastructure.Auth.Services;
using BudgetManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace BudgetManager.IntegrationTests.Api;

public abstract class BaseTest(ITestOutputHelper testOutputHelper, ApiFixture fixture) : TestBed<ApiFixture>(testOutputHelper, fixture)
{
  protected HttpClient Client => _fixture.GetService<HttpClient>(_testOutputHelper) ?? throw new InvalidOperationException($"{nameof(HttpClient)} is not registered in the service collection.");
  protected ApplicationDbContext GetContext()
  {
    var dbContext = GetServiceProvider().GetService<ApplicationDbContext>()
      ?? throw new InvalidOperationException($"{nameof(ApplicationDbContext)} is not registered in the service collection.");

    dbContext.Database.EnsureCreated();

    return dbContext;
  }

  protected string GetValidToken()
  {
    var tokenGenerator = GetServiceProvider().GetService<IJwtTokenGenerator>()
      ?? throw new InvalidOperationException($"{nameof(IJwtTokenGenerator)} is not registered in the service collection.");


    return tokenGenerator.GenerateToken(new(Guid.NewGuid(), "Test User", $"test@email{Guid.NewGuid()}"));
  }
  private IServiceProvider GetServiceProvider()
  {
    var scope = _fixture.GetServiceProvider(_testOutputHelper).CreateScope();
    var factory = scope.ServiceProvider.GetService<WebApplicationFactory<Program>>()
      ?? throw new InvalidOperationException($"{nameof(WebApplicationFactory<Program>)} is not registered in the service collection.");
    return factory.Services;
  }
}
