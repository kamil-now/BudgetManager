using System.Net;
using System.Net.Http.Json;
using BudgetManager.Application.Commands;
using BudgetManager.Application.Models;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Api.Accounts;

public class UpdateAccountTests(ITestOutputHelper testOutputHelper, ApiFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    private sealed record LedgerShape(Guid Id, AccountShape[] Accounts);
    private sealed record AccountShape(Guid Id, string Name);

    [Fact]
    public async Task UpdateAccount_WhenUserIsUnauthorized_401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.PutAsJsonAsync($"/api/accounts/{Guid.NewGuid()}", new { name = "Bank" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateAccount_WhenValid_204_AndRenamesIt()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerAsync();
        var accountId = ledger.Accounts.Single().Id;

        // Act
        var response = await Client.PutAsJsonAsync($"/api/accounts/{accountId}", new { name = "Bank" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
        var account = await Client.GetFromJsonAsync<AccountShape>($"/api/accounts/{accountId}");
        account.ShouldNotBeNull().Name.ShouldBe("Bank");
    }

    [Fact]
    public async Task UpdateAccount_WhenNameIsEmpty_422()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerAsync();

        // Act
        var response = await Client.PutAsJsonAsync($"/api/accounts/{ledger.Accounts.Single().Id}", new { name = "" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateAccount_WhenNameIsTaken_409()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerAsync();
        var post = await Client.PostAsJsonAsync("/api/accounts", new CreateAccountCommand(ledger.Id, new(0, "EUR"), "Bank"));
        var otherId = await post.Content.ReadFromJsonAsync<Guid>();

        // Act
        var response = await Client.PutAsJsonAsync($"/api/accounts/{otherId}", new { name = "[account name]" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateAccount_WhenAccountBelongsToAnotherUser_403()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerAsync();

        await RegisterAndLogin();

        // Act
        var response = await Client.PutAsJsonAsync($"/api/accounts/{ledger.Accounts.Single().Id}", new { name = "Bank" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private async Task<LedgerShape> CreateLedgerAsync()
    {
        var createResponse = await Client.PostAsJsonAsync("/api/ledgers", new CreateLedgerCommand(
            "[ledger name]",
            [new("[budget name]", [new CreateFundDTO("Food")])],
            [new(new(0, "EUR"), "[account name]")]));
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await createResponse.Content.ReadAsStringAsync());
        var ledgerId = await createResponse.Content.ReadFromJsonAsync<Guid>();
        return (await Client.GetFromJsonAsync<LedgerShape>($"/api/ledgers/{ledgerId}"))!;
    }
}
