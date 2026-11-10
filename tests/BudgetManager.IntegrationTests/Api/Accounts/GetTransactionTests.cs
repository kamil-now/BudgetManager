using System.Net;
using System.Net.Http.Json;
using BudgetManager.Application.Commands;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Api.Accounts;

public class GetTransactionTests(ITestOutputHelper testOutputHelper, ApiFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    private sealed record LedgerShape(Guid Id, AccountShape[] Accounts);
    private sealed record AccountShape(Guid Id);
    private sealed record TransactionShape(Guid Id, Guid AccountId, string? Title);

    [Fact]
    public async Task GetTransaction_WhenUserIsUnauthorized_401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.GetAsync($"/api/accounts/{Guid.NewGuid()}/transaction/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTransaction_WhenTransactionBelongsToUser_200()
    {
        // Arrange
        await RegisterAndLogin();
        var accountId = await CreateLedgerAndGetAccountId();
        var transactionId = await CreateTransaction(accountId);

        // Act
        var response = await Client.GetAsync($"/api/accounts/{accountId}/transaction/{transactionId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var transaction = await response.Content.ReadFromJsonAsync<TransactionShape>();
        transaction.ShouldNotBeNull();
        transaction.Id.ShouldBe(transactionId);
        transaction.AccountId.ShouldBe(accountId);
        transaction.Title.ShouldBe("Groceries");
    }

    [Fact]
    public async Task GetTransaction_WhenTransactionBelongsToAnotherUser_403()
    {
        // Arrange
        await RegisterAndLogin();
        var accountId = await CreateLedgerAndGetAccountId();
        var transactionId = await CreateTransaction(accountId);

        await RegisterAndLogin();

        // Act
        var response = await Client.GetAsync($"/api/accounts/{accountId}/transaction/{transactionId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetTransaction_WhenTransactionBelongsToAnotherAccount_404()
    {
        // Arrange
        await RegisterAndLogin();
        var accountId = await CreateLedgerAndGetAccountId();
        var otherAccountId = await CreateLedgerAndGetAccountId();
        var transactionId = await CreateTransaction(otherAccountId);

        // Act
        var response = await Client.GetAsync($"/api/accounts/{accountId}/transaction/{transactionId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTransaction_WhenResourceDoesNotExist_403()
    {
        // Arrange
        await RegisterAndLogin();
        var accountId = await CreateLedgerAndGetAccountId();

        // Act
        var response = await Client.GetAsync($"/api/accounts/{accountId}/transaction/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private async Task<Guid> CreateTransaction(Guid accountId)
    {
        var response = await Client.PostAsJsonAsync(
            $"/api/accounts/{accountId}/transaction",
            new CreateAccountTransactionCommand(accountId, new(25, "EUR"), DateTimeOffset.UtcNow, "Groceries"));
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<Guid>();
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
