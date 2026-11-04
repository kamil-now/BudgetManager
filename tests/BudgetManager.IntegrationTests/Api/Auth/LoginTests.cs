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
    public async Task Login_WhenEmailIsUnknown_ShouldReturnGenericMessage()
    {
        // Act
        var response = await Client.PostAsJsonAsync(_url, new LoginCommand($"unknown{Guid.NewGuid()}@email.com", "password"));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).ShouldBe("Invalid email or password.");
    }

    [Fact]
    public async Task Login_WhenPasswordIsWrong_ShouldReturnGenericMessage()
    {
        // Arrange
        var email = $"test{Guid.NewGuid()}@email.com";
        await Client.PostAsJsonAsync("/api/auth/register", new CreateUserCommand(email, "password", null));

        // Act
        var response = await Client.PostAsJsonAsync(_url, new LoginCommand(email, "wrong-password"));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).ShouldBe("Invalid email or password.");
    }

    [Fact]
    public async Task Login_WhenEmailDiffersInCaseAndWhitespace_ShouldSucceed()
    {
        // Arrange
        var email = $"test{Guid.NewGuid()}@email.com";
        await Client.PostAsJsonAsync("/api/auth/register", new CreateUserCommand(email, "password", null));

        // Act
        var response = await Client.PostAsJsonAsync(_url, new LoginCommand($"  {email.ToUpperInvariant()} ", "password"));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}

