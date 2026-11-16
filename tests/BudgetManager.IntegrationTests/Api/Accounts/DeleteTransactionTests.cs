using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudgetManager.Application.Commands;
using BudgetManager.Application.Models;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Api.Accounts;

public class DeleteTransactionTests(ITestOutputHelper testOutputHelper, ApiFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    private sealed record LedgerShape(Guid Id, AccountShape[] Accounts);
    private sealed record AccountShape(Guid Id);

    [Fact]
    public async Task DeleteTransaction_WhenUserIsUnauthorized_401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.DeleteAsync($"/api/accounts/{Guid.NewGuid()}/transaction/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteTransaction_WhenTransactionBelongsToUser_204()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerAsync(0);
        var accountId = ledger.Accounts.Single().Id;
        var transactionId = await CreateTransactionAsync(accountId);

        // Act
        var response = await Client.DeleteAsync($"/api/accounts/{accountId}/transaction/{transactionId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
        (await Client.GetAsync($"/api/accounts/{accountId}/transaction/{transactionId}")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteTransaction_WhenTransactionBelongsToAnotherUser_403()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerAsync(0);
        var accountId = ledger.Accounts.Single().Id;
        var transactionId = await CreateTransactionAsync(accountId);

        await RegisterAndLogin();

        // Act
        var response = await Client.DeleteAsync($"/api/accounts/{accountId}/transaction/{transactionId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteTransaction_WhenTransactionBelongsToAnotherAccount_404()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerAsync(0);
        var accountId = ledger.Accounts.Single().Id;
        var transactionId = await CreateTransactionAsync(accountId);
        var otherResponse = await Client.PostAsJsonAsync("/api/accounts", new CreateAccountCommand(ledger.Id, new(0, "EUR"), "Other"));
        otherResponse.StatusCode.ShouldBe(HttpStatusCode.Created, await otherResponse.Content.ReadAsStringAsync());
        var otherAccountId = await otherResponse.Content.ReadFromJsonAsync<Guid>();

        // Act
        var response = await Client.DeleteAsync($"/api/accounts/{otherAccountId}/transaction/{transactionId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTransaction_WhenTransactionIsInitialBalance_204()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerAsync(100);
        var accountId = ledger.Accounts.Single().Id;
        var feed = await Client.GetFromJsonAsync<JsonElement>($"/api/ledgers/{ledger.Id}/feed?types=Income");
        var initialBalanceId = feed.GetProperty("items").EnumerateArray().Single(x => x.GetProperty("isInitialBalance").GetBoolean()).GetProperty("id").GetGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/accounts/{accountId}/transaction/{initialBalanceId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
    }

    private async Task<Guid> CreateTransactionAsync(Guid accountId)
    {
        var response = await Client.PostAsJsonAsync($"/api/accounts/{accountId}/transaction",
            new CreateAccountTransactionCommand(accountId, new(-40, "EUR"), DateTimeOffset.UtcNow, "Groceries"));
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task<LedgerShape> CreateLedgerAsync(decimal initialBalance)
    {
        var createResponse = await Client.PostAsJsonAsync("/api/ledgers", new CreateLedgerCommand(
            "[ledger name]",
            [new("[budget name]", [new CreateFundDTO("Food")])],
            [new(new(initialBalance, "EUR"), "[account name]")],
            initialBalance > 0 ? [new InitialAllocationDTO("[budget name]", "Food", new(initialBalance, "EUR"))] : null));
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await createResponse.Content.ReadAsStringAsync());
        var ledgerId = await createResponse.Content.ReadFromJsonAsync<Guid>();
        return (await Client.GetFromJsonAsync<LedgerShape>($"/api/ledgers/{ledgerId}"))!;
    }
}
