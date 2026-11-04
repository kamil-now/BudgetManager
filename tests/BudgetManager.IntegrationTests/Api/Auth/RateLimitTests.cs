using System.Net;
using System.Net.Http.Json;
using BudgetManager.Application.Commands;
using Microsoft.AspNetCore.Hosting;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Api.Auth;

public class RateLimitTests(ITestOutputHelper testOutputHelper, ApiFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task Login_WhenLimitIsExceeded_ShouldReturnTooManyRequests()
    {
        // Arrange
        using var client = _fixture.CreateClient(builder => builder.UseSetting("AuthRateLimit:PermitLimit", "2"));
        var command = new LoginCommand($"unknown{Guid.NewGuid()}@email.com", "password");
        await client.PostAsJsonAsync("/api/auth/login", command);
        await client.PostAsJsonAsync("/api/auth/login", command);

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", command);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }
}
