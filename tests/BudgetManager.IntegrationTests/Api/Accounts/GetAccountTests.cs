using System.Net;
using System.Net.Http.Json;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Api.Accounts;

public class GetAccountTests(ITestOutputHelper testOutputHelper, ApiFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    private sealed record LedgerShape(Guid Id, AccountShape[] Accounts);
    private sealed record AccountShape(Guid Id);

    [Fact]
    public async Task GetAccount_WhenUserIsUnauthorized_401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.GetAsync($"/api/accounts/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAccount_WhenAccountBelongsToUser_200()
    {
        // Arrange
        await RegisterAndLogin();
        var accountId = await CreateLedgerAndGetAccountId();

        // Act
        var response = await Client.GetAsync($"/api/accounts/{accountId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var account = await response.Content.ReadFromJsonAsync<AccountShape>();
        account.ShouldNotBeNull();
        account.Id.ShouldBe(accountId);
    }

    [Fact]
    public async Task GetAccount_WhenAccountBelongsToAnotherUser_403()
    {
        // Arrange
        await RegisterAndLogin();
        var accountId = await CreateLedgerAndGetAccountId();

        await RegisterAndLogin();

        // Act
        var response = await Client.GetAsync($"/api/accounts/{accountId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAccount_WhenResourceDoesNotExist_403()
    {
        // Arrange
        await RegisterAndLogin();

        // Act
        var response = await Client.GetAsync($"/api/accounts/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private async Task<Guid> CreateLedgerAndGetAccountId()
    {
        var createResponse = await Client.PostAsJsonAsync("/api/ledgers", Ledgers.LedgersControllerTests.ValidCommand);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await createResponse.Content.ReadAsStringAsync());
        var ledgerId = await createResponse.Content.ReadFromJsonAsync<Guid>();
        var getResponse = await Client.GetAsync($"/api/ledgers/{ledgerId}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await getResponse.Content.ReadAsStringAsync());
        var ledger = await getResponse.Content.ReadFromJsonAsync<LedgerShape>();
        return ledger!.Accounts.First().Id;
    }
}
