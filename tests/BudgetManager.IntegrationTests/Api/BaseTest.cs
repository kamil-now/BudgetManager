using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BudgetManager.Api.Models;
using BudgetManager.Application.Commands;
using BudgetManager.Application.Models;
using BudgetManager.Infrastructure.Persistence;
using Shouldly;
using Xunit.Abstractions;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace BudgetManager.IntegrationTests.Api;

public abstract class BaseTest(ITestOutputHelper testOutputHelper, ApiFixture fixture) : TestBed<ApiFixture>(testOutputHelper, fixture)
{
    protected HttpClient Client => _fixture.GetService<HttpClient>(_testOutputHelper)
     ?? throw new InvalidOperationException($"{nameof(HttpClient)} is not registered in the service collection.");
    protected ApplicationDbContext DbContext => _fixture.GetService<ApplicationDbContext>(_testOutputHelper)
     ?? throw new InvalidOperationException($"{nameof(ApplicationDbContext)} is not registered in the service collection.");

    protected async Task AssertPostFailsWhenUnauthorized(string endpoint)
    {
        Client.DefaultRequestHeaders.Authorization = null;
        var response = await Client.PostAsJsonAsync(endpoint, new { });
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    protected async Task AssertUnauthorizedWhenTokenIsInvalid(string endpoint)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid_token");
        var response = await Client.PostAsJsonAsync(endpoint, new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    protected async Task<Guid> RegisterAndLogin()
    {
        var user = new CreateUserCommand($"test{Guid.NewGuid()}@email.com", "password", "Test User");
        var userId = await Register(user);
        await Login(new(user.Email, user.Password));
        return userId;
    }

    protected async Task<CreateUserCommand> RegisterAndLogin(UserPreferencesDTO? preferences)
    {
        var user = new CreateUserCommand($"test{Guid.NewGuid()}@email.com", "password", "Test User", preferences);
        await Register(user);
        await Login(new(user.Email, user.Password));
        return user;
    }

    private async Task<Guid> Register(CreateUserCommand command)
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register", command);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var id = await response.Content.ReadFromJsonAsync<Guid>();

        id.ShouldNotBe(Guid.Empty);

        return id;
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
}
