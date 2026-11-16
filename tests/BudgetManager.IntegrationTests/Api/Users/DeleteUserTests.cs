using System.Net;
using System.Net.Http.Json;
using BudgetManager.Application.Commands;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Api.Users;

public class DeleteUserTests(ITestOutputHelper testOutputHelper, ApiFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    private const string Url = "/api/users/me";

    private Task<HttpResponseMessage> DeleteAsync(object body)
        => Client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, Url) { Content = JsonContent.Create(body) });

    [Fact]
    public async Task DeleteUser_WhenUserIsUnauthorized_401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await DeleteAsync(new { password = "password" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteUser_WhenPasswordIsMissing_400()
    {
        // Arrange
        await RegisterAndLogin();

        // Act
        var response = await DeleteAsync(new { });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteUser_WhenPasswordIsWrong_422_AndKeepsUser()
    {
        // Arrange
        var userId = await RegisterAndLogin();

        // Act
        var response = await DeleteAsync(new { password = "wrong password" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await DbContext.Users.AsNoTracking().AnyAsync(x => x.Id == userId)).ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteUser_WhenPasswordIsCorrect_204_AndRemovesUser()
    {
        // Arrange
        var userId = await RegisterAndLogin();

        // Act
        var response = await DeleteAsync(new { password = "password" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
        (await DbContext.Users.AsNoTracking().AnyAsync(x => x.Id == userId)).ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteUser_WhenUserWasDeleted_TokenNoLongerWorks_401()
    {
        // Arrange
        await RegisterAndLogin();
        (await DeleteAsync(new { password = "password" })).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Act
        var response = await Client.GetAsync("/api/ledgers");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteUser_WhenUserWasDeleted_LoginFails_401()
    {
        // Arrange
        var user = await RegisterAndLogin(preferences: null);
        (await DeleteAsync(new { password = user.Password })).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", new LoginCommand(user.Email, user.Password));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteUser_WhenUserHasLedger_204_AndRemovesIt()
    {
        // Arrange
        await RegisterAndLogin();
        var ledgerResponse = await Client.PostAsJsonAsync("/api/ledgers", Ledgers.LedgersControllerTests.ValidCommand);
        var ledgerId = await ledgerResponse.Content.ReadFromJsonAsync<Guid>();

        // Act
        var response = await DeleteAsync(new { password = "password" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
        (await DbContext.Ledgers.AsNoTracking().AnyAsync(x => x.Id == ledgerId)).ShouldBeFalse();
    }
}
