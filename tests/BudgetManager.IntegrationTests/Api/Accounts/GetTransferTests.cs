using System.Net;
using System.Net.Http.Json;
using BudgetManager.Application.Commands;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Api.Accounts;

public class GetTransferTests(ITestOutputHelper testOutputHelper, ApiFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    private sealed record LedgerShape(Guid Id, AccountShape[] Accounts);
    private sealed record AccountShape(Guid Id);
    private sealed record TransferShape(Guid Id, Guid AccountId, Guid? TargetAccountId);

    [Fact]
    public async Task GetTransfer_WhenUserIsUnauthorized_401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.GetAsync($"/api/accounts/{Guid.NewGuid()}/transfer/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTransfer_WhenExchangeBelongsToUser_200()
    {
        // Arrange
        await RegisterAndLogin();
        var accountId = await CreateLedgerAndGetAccountId();
        var transferId = await CreateExchange(accountId);

        // Act
        var response = await Client.GetAsync($"/api/accounts/{accountId}/transfer/{transferId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var transfer = await response.Content.ReadFromJsonAsync<TransferShape>();
        transfer.ShouldNotBeNull();
        transfer.Id.ShouldBe(transferId);
        transfer.AccountId.ShouldBe(accountId);
    }

    [Fact]
    public async Task GetTransfer_WhenExchangeBelongsToAnotherUser_403()
    {
        // Arrange
        await RegisterAndLogin();
        var accountId = await CreateLedgerAndGetAccountId();
        var transferId = await CreateExchange(accountId);

        await RegisterAndLogin();

        // Act
        var response = await Client.GetAsync($"/api/accounts/{accountId}/transfer/{transferId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetTransfer_WhenResourceDoesNotExist_403()
    {
        // Arrange
        await RegisterAndLogin();
        var accountId = await CreateLedgerAndGetAccountId();

        // Act
        var response = await Client.GetAsync($"/api/accounts/{accountId}/transfer/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetTransfer_WhenTransferDoesNotInvolveAccount_404()
    {
        // Arrange
        await RegisterAndLogin();
        var (ledgerId, accountId) = await CreateLedgerAndGetIds();
        var transferId = await CreateExchange(accountId);
        var accountResponse = await Client.PostAsJsonAsync("/api/accounts", new CreateAccountCommand(ledgerId, new(0, "EUR"), $"Other {Guid.NewGuid()}"));
        accountResponse.StatusCode.ShouldBe(HttpStatusCode.Created, await accountResponse.Content.ReadAsStringAsync());
        var otherAccountId = await accountResponse.Content.ReadFromJsonAsync<Guid>();

        // Act
        var response = await Client.GetAsync($"/api/accounts/{otherAccountId}/transfer/{transferId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private async Task<Guid> CreateExchange(Guid accountId)
    {
        var response = await Client.PostAsJsonAsync(
            $"/api/accounts/{accountId}/currencyExchange",
            new CreateCurrencyExchangeCommand(accountId, null, new(90, "EUR"), new(100, "USD"), DateTimeOffset.UtcNow, null));
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task<Guid> CreateLedgerAndGetAccountId() => (await CreateLedgerAndGetIds()).AccountId;

    private async Task<(Guid LedgerId, Guid AccountId)> CreateLedgerAndGetIds()
    {
        var createResponse = await Client.PostAsJsonAsync("/api/ledgers", Ledgers.LedgersControllerTests.ValidCommand);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await createResponse.Content.ReadAsStringAsync());
        var ledgerId = await createResponse.Content.ReadFromJsonAsync<Guid>();
        var getResponse = await Client.GetAsync($"/api/ledgers/{ledgerId}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await getResponse.Content.ReadAsStringAsync());
        var ledger = await getResponse.Content.ReadFromJsonAsync<LedgerShape>();
        return (ledgerId, ledger!.Accounts.First().Id);
    }
}
