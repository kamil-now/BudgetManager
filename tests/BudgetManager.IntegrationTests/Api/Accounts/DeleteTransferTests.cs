using System.Net;
using System.Net.Http.Json;
using BudgetManager.Application.Commands;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Api.Accounts;

public class DeleteTransferTests(ITestOutputHelper testOutputHelper, ApiFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task DeleteTransfer_WhenUserIsUnauthorized_401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.DeleteAsync($"/api/accounts/{Guid.NewGuid()}/transfer/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteTransfer_WhenExchangeBelongsToUser_204()
    {
        // Arrange
        await RegisterAndLogin();
        var accountId = await CreateLedgerAndGetAccountIdAsync();
        var transferId = await CreateExchangeAsync(accountId);

        // Act
        var response = await Client.DeleteAsync($"/api/accounts/{accountId}/transfer/{transferId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
        (await Client.GetAsync($"/api/accounts/{accountId}/transfer/{transferId}")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteTransfer_WhenExchangeBelongsToAnotherUser_403()
    {
        // Arrange
        await RegisterAndLogin();
        var accountId = await CreateLedgerAndGetAccountIdAsync();
        var transferId = await CreateExchangeAsync(accountId);

        await RegisterAndLogin();

        // Act
        var response = await Client.DeleteAsync($"/api/accounts/{accountId}/transfer/{transferId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteTransfer_WhenTransferDoesNotExist_403()
    {
        // Arrange
        await RegisterAndLogin();
        var accountId = await CreateLedgerAndGetAccountIdAsync();

        // Act
        var response = await Client.DeleteAsync($"/api/accounts/{accountId}/transfer/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private async Task<Guid> CreateExchangeAsync(Guid accountId)
    {
        var response = await Client.PostAsJsonAsync(
            $"/api/accounts/{accountId}/currencyExchange",
            new CreateCurrencyExchangeCommand(accountId, null, new(90, "EUR"), new(100, "USD"), DateTimeOffset.UtcNow, null));
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task<Guid> CreateLedgerAndGetAccountIdAsync()
    {
        var createResponse = await Client.PostAsJsonAsync("/api/ledgers", Ledgers.LedgersControllerTests.ValidCommand);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await createResponse.Content.ReadAsStringAsync());
        var ledgerId = await createResponse.Content.ReadFromJsonAsync<Guid>();
        var ledger = await Client.GetFromJsonAsync<LedgerShape>($"/api/ledgers/{ledgerId}");
        return ledger!.Accounts.First().Id;
    }

    private sealed record LedgerShape(Guid Id, AccountShape[] Accounts);
    private sealed record AccountShape(Guid Id);
}
