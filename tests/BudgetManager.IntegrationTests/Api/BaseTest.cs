using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BudgetManager.Api.Models;
using BudgetManager.Application.Commands;
using BudgetManager.Infrastructure.Auth.Interfaces;
using BudgetManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
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

  protected async Task AssertPostFailsWhenUnauthorized(string endpoint)
  {
    Client.DefaultRequestHeaders.Authorization = null;
    var response = await Client.PostAsJsonAsync(endpoint, new { });
    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }
  public async Task AssertFailsWhenTokenIsInvalid(string endpoint)
  {
    Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid_token");
    var response = await Client.PostAsJsonAsync(endpoint, new { });
    
    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  protected string GetValidToken()
  {
    var tokenGenerator = GetServiceProvider().GetService<IJwtTokenGenerator>()
      ?? throw new InvalidOperationException($"{nameof(IJwtTokenGenerator)} is not registered in the service collection.");


    return tokenGenerator.GenerateToken(new(Guid.NewGuid(), "Test User", $"test@email{Guid.NewGuid()}"));
  }

  protected async Task RegisterAndLogin()
  {
    var user = new CreateUserCommand($"test@email{Guid.NewGuid()}", "password", "Test User");
    await Register(user);
    await Login(new(user.Email, user.Password));
  }

  private async Task Register(CreateUserCommand command)
  {
    var response = await Client.PostAsJsonAsync("/api/auth/register", command);

    response.StatusCode.ShouldBe(HttpStatusCode.OK);

    var id = await response.Content.ReadAsStringAsync();

    id.ShouldNotBeEmpty();
    id.ShouldNotBe(Guid.Empty.ToString());
  }

  private async Task Login(LoginCommand command)
  {
    var response = await Client.PostAsJsonAsync("/api/auth/login", command);

    response.StatusCode.ShouldBe(HttpStatusCode.OK);

    var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();

    tokenResponse.ShouldNotBeNull();

    var token = tokenResponse.Token;

    token.ShouldNotBeEmpty();

    Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
  }

  private IServiceProvider GetServiceProvider()
  {
    var scope = _fixture.GetServiceProvider(_testOutputHelper).CreateScope();
    var factory = scope.ServiceProvider.GetService<WebApplicationFactory<Program>>()
      ?? throw new InvalidOperationException($"{nameof(WebApplicationFactory<Program>)} is not registered in the service collection.");
    return factory.Services;
  }
}
