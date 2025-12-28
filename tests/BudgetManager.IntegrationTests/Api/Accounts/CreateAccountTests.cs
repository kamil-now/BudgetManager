using System.Net;
using System.Net.Http.Json;
using BudgetManager.Application.Commands;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Api.Accounts;

public class CreateAccountTests(ITestOutputHelper testOutputHelper, ApiFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    private readonly string _url = "/api/accounts";
    [Fact]
    public async Task CreateAccount_WhenUserIsUnauthorized_Fails()
    {
        // Act
        var response = await Client.PostAsJsonAsync(_url, new { });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateAccount_WhenBearerTokenIsInvalid_Fails()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid_token");

        // Act
        var response = await Client.PostAsJsonAsync(_url, new { });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }


    [Fact]
    public async Task CreateAccount_WhenBearerTokenIsValid_Succeeds()
    {
        // Arrange

        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GetValidToken());

        // Act
        var response = await Client.PostAsJsonAsync(_url, new { });

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
        var response = await Client.PostAsJsonAsync(_url, request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var responseContent = await response.Content.ReadFromJsonAsync<Guid>();
        responseContent.ShouldNotBe(Guid.Empty);
    }
}
