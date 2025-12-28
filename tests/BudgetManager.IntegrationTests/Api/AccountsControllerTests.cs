using System.Net;
using System.Net.Http.Json;
using BudgetManager.Application.Commands;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Api;

public class AccountsControllerTests(ITestOutputHelper testOutputHelper, ApiFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task CreateAccount_WhenUserIsUnauthorized_Fails()
    {
        // Act
        var response = await Client.PostAsJsonAsync("/api/accounts", new { });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateAccount_WhenBearerTokenIsInvalid_Fails()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid_token");

        // Act
        var response = await Client.PostAsJsonAsync("/api/accounts", new { });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }


    [Fact]
    public async Task CreateAccount_WhenBearerTokenIsValid_Succeeds()
    {
        // Arrange

        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GetValidToken());

        // Act
        var response = await Client.PostAsJsonAsync("/api/accounts", new { });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateAccount_WhenValidRequest_ReturnsCreated()
    {
        // Arrange
        await RegisterAndLogin();

        var request = new CreateAccountCommand(
          null,
          new(100, "PLN"),
          "Test Account",
          "Test Account Description");

        // Act
        var response = await Client.PostAsJsonAsync("/api/accounts", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
