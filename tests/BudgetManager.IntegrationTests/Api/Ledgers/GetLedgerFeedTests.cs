using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudgetManager.Application.Commands;
using BudgetManager.Application.Models;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Api.Ledgers;

public class GetLedgerFeedTests(ITestOutputHelper testOutputHelper, ApiFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    private sealed record LedgerShape(Guid Id, AccountShape[] Accounts);
    private sealed record AccountShape(Guid Id);

    [Fact]
    public async Task GetLedgerFeed_WhenUserIsUnauthorized_401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.GetAsync($"/api/ledgers/{Guid.NewGuid()}/feed");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetLedgerFeed_WhenLedgerBelongsToAnotherUser_403()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerAsync();

        await RegisterAndLogin();

        // Act
        var response = await Client.GetAsync($"/api/ledgers/{ledger.Id}/feed");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetLedgerFeed_WhenLedgerHasTransactions_200_WithCamelCasePageAndNumericTypes()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerAsync();
        var accountId = ledger.Accounts.Single().Id;
        await CreateTransactionAsync(accountId, -40, "Groceries", DateTimeOffset.UtcNow.AddDays(-1));
        var incomeId = await CreateTransactionAsync(accountId, 100, "Salary");

        // Act
        var response = await Client.GetAsync($"/api/ledgers/{ledger.Id}/feed?limit=1");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("total").GetInt32().ShouldBe(2);
        json.RootElement.GetProperty("offset").GetInt32().ShouldBe(0);
        json.RootElement.GetProperty("limit").GetInt32().ShouldBe(1);
        var item = json.RootElement.GetProperty("items").EnumerateArray().ShouldHaveSingleItem();
        item.GetProperty("id").GetGuid().ShouldBe(incomeId);
        item.GetProperty("type").GetInt32().ShouldBe(0);
        item.GetProperty("value").GetProperty("amount").GetDecimal().ShouldBe(100);
        item.GetProperty("accountName").GetString().ShouldBe("[account name]");
        item.GetProperty("isInitialBalance").GetBoolean().ShouldBeFalse();
        item.GetProperty("allocated").GetDecimal().ShouldBe(0);
        item.GetProperty("allocations").GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task GetLedgerFeed_WhenTypesAreRepeatedQueryParameters_200_WithOnlyThoseTypes()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerAsync();
        var accountId = ledger.Accounts.Single().Id;
        await CreateTransactionAsync(accountId, 100, "Salary");
        var expenseId = await CreateTransactionAsync(accountId, -40, "Groceries");

        // Act
        var response = await Client.GetAsync($"/api/ledgers/{ledger.Id}/feed?types=Expense&types=Transfer");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var page = await response.Content.ReadFromJsonAsync<LedgerFeedPageDTO>();
        page.ShouldNotBeNull().Items.ShouldHaveSingleItem().Id.ShouldBe(expenseId);
    }

    [Fact]
    public async Task GetLedgerFeed_WhenFromIsLaterThanTo_422()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerAsync();

        // Act
        var response = await Client.GetAsync($"/api/ledgers/{ledger.Id}/feed?from=2026-02-01T00:00:00Z&to=2026-01-01T00:00:00Z");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetLedgerFeed_WhenLimitIsTooLarge_422()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerAsync();

        // Act
        var response = await Client.GetAsync($"/api/ledgers/{ledger.Id}/feed?limit=201");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    private async Task<Guid> CreateTransactionAsync(Guid accountId, decimal amount, string title, DateTimeOffset? date = null)
    {
        var response = await Client.PostAsJsonAsync($"/api/accounts/{accountId}/transaction",
            new CreateAccountTransactionCommand(accountId, new(amount, "EUR"), date ?? DateTimeOffset.UtcNow, title));
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<Guid>();
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
