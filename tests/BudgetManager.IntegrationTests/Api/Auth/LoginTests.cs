using System.Net;
using System.Net.Http.Json;
using BudgetManager.Application.Commands;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Api.Auth;

public class LoginTests(ITestOutputHelper testOutputHelper, ApiFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    private readonly string _url = "/api/auth/login";

    [Theory]
    [InlineData(null, null)]
    [InlineData("email", null)]
    [InlineData(null, "password")]
    public async Task Login_WhenRequestIsInvalid_ShouldFail(string? email, string? password)
    {
        // Act
        var response = await Client.PostAsJsonAsync(_url, new { Email = email, Password = password });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WhenEmailIsInvalid_ShouldFail()
    {
        // Act
        var response = await Client.PostAsJsonAsync(_url, new LoginCommand("email", "password"));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.ShouldBe($"User with email 'email' not found.");
    }
}

