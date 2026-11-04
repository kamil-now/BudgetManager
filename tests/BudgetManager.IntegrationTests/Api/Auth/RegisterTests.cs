using System.Net;
using System.Net.Http.Json;
using BudgetManager.Application.Commands;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Api.Auth;

public class RegisterTests(ITestOutputHelper testOutputHelper, ApiFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    private readonly string _url = "/api/auth/register";

    [Fact]
    public async Task Register_WhenNameIsNull_ShouldCreateUserWithoutName()
    {
        // Arrange
        var command = new CreateUserCommand($"test{Guid.NewGuid()}@email.com", "password", null);

        // Act
        var response = await Client.PostAsJsonAsync(_url, command);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        var user = await DbContext.Users.AsNoTracking().SingleAsync(x => x.Id == id);
        user.Name.ShouldBeNull();
    }

    [Fact]
    public async Task Register_WhenEmailHasUpperCaseAndWhitespace_ShouldStoreItNormalized()
    {
        // Arrange
        var email = $"test{Guid.NewGuid()}@email.com";
        var command = new CreateUserCommand($" {email.ToUpperInvariant()}  ", "password", null);

        // Act
        var response = await Client.PostAsJsonAsync(_url, command);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        var user = await DbContext.Users.AsNoTracking().SingleAsync(x => x.Id == id);
        user.Email.ShouldBe(email);
    }

    [Fact]
    public async Task Register_WhenEmailDiffersOnlyInCase_ShouldReturnConflict()
    {
        // Arrange
        var email = $"test{Guid.NewGuid()}@email.com";
        await Client.PostAsJsonAsync(_url, new CreateUserCommand(email, "password", null));

        // Act
        var response = await Client.PostAsJsonAsync(_url, new CreateUserCommand(email.ToUpperInvariant(), "password", null));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("plainaddress")]
    [InlineData("test@email")]
    [InlineData("test @email.com")]
    public async Task Register_WhenEmailIsInvalid_ShouldReturnUnprocessableEntity(string email)
    {
        // Act
        var response = await Client.PostAsJsonAsync(_url, new CreateUserCommand(email, "password", null));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Theory]
    [InlineData("1234567")]
    [InlineData("ąąąąąąąąąąąąąąąąąąąąąąąąąąąąąąąąąąąąą")] // 37 characters, 74 bytes
    public async Task Register_WhenPasswordIsInvalid_ShouldReturnUnprocessableEntity(string password)
    {
        // Act
        var response = await Client.PostAsJsonAsync(_url, new CreateUserCommand($"test{Guid.NewGuid()}@email.com", password, null));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }
}
