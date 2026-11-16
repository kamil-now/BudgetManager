using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudgetManager.Application.Commands;
using BudgetManager.Application.Models;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Api.Ledgers;

public class UpdateAllocationTests(ITestOutputHelper testOutputHelper, ApiFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    private sealed record LedgerShape(Guid Id, BudgetShape[] Budgets, AccountShape[] Accounts);
    private sealed record BudgetShape(Guid Id, FundShape[] Funds);
    private sealed record FundShape(Guid Id, string Name);
    private sealed record AccountShape(Guid Id);

    private static string Url(Guid ledgerId, Guid allocationId) => $"/api/ledgers/{ledgerId}/allocations/{allocationId}";

    private static object Body(Guid fundId, decimal amount = 40m) => new
    {
        currency = "EUR",
        amount,
        allocation = new { lines = new[] { new { fundId, sequence = 0, type = 0, amount } } },
        date = new DateTimeOffset(2026, 4, 2, 0, 0, 0, TimeSpan.Zero)
    };

    [Fact]
    public async Task UpdateAllocation_WhenUserIsUnauthorized_401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.PutAsJsonAsync(Url(Guid.NewGuid(), Guid.NewGuid()), new { });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateAllocation_WhenRequestIsValid_200_AndReplacesTheAllocation()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerWithIncomeAsync();
        var allocationId = await AllocateAsync(ledger, 25);

        // Act
        var response = await Client.PutAsJsonAsync(Url(ledger.Id, allocationId), Body(Food(ledger), 40));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var feed = await Client.GetFromJsonAsync<JsonElement>($"/api/ledgers/{ledger.Id}/feed?types=4");
        var item = feed.GetProperty("items").EnumerateArray().ShouldHaveSingleItem();
        item.GetProperty("id").GetGuid().ShouldBe(allocationId);
        item.GetProperty("value").GetProperty("amount").GetDecimal().ShouldBe(40);
    }

    [Fact]
    public async Task UpdateAllocation_WhenAllocationBelongsToAnotherUser_403()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerWithIncomeAsync();
        var allocationId = await AllocateAsync(ledger, 25);

        await RegisterAndLogin();

        // Act
        var response = await Client.PutAsJsonAsync(Url(ledger.Id, allocationId), Body(Food(ledger)));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateAllocation_WhenAllocationDoesNotExist_403()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerWithIncomeAsync();

        // Act
        var response = await Client.PutAsJsonAsync(Url(ledger.Id, Guid.NewGuid()), Body(Food(ledger)));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateAllocation_WhenLineFundBelongsToAnotherUser_403()
    {
        // Arrange
        await RegisterAndLogin();
        var otherUsersLedger = await CreateLedgerWithIncomeAsync();

        await RegisterAndLogin();
        var ledger = await CreateLedgerWithIncomeAsync();
        var allocationId = await AllocateAsync(ledger, 25);

        // Act
        var response = await Client.PutAsJsonAsync(Url(ledger.Id, allocationId), Body(Food(otherUsersLedger)));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateAllocation_WhenAmountExceedsUnallocated_422()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerWithIncomeAsync();
        var allocationId = await AllocateAsync(ledger, 25);

        // Act
        var response = await Client.PutAsJsonAsync(Url(ledger.Id, allocationId), Body(Food(ledger), 100.01m));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    private static Guid Food(LedgerShape ledger) => ledger.Budgets.Single().Funds.Single(x => x.Name == "Food").Id;

    private async Task<Guid> AllocateAsync(LedgerShape ledger, decimal amount)
    {
        var response = await Client.PostAsJsonAsync($"/api/ledgers/{ledger.Id}/allocations", new
        {
            currency = "EUR",
            allocation = new { lines = new[] { new { fundId = Food(ledger), sequence = 0, type = 0, amount } } }
        });
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task<LedgerShape> CreateLedgerWithIncomeAsync()
    {
        var createResponse = await Client.PostAsJsonAsync("/api/ledgers", new CreateLedgerCommand(
            "[ledger name]",
            [new("[budget name]", [new CreateFundDTO("Food")])],
            [new(new(0, "EUR"), "[account name]")]));
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await createResponse.Content.ReadAsStringAsync());
        var ledgerId = await createResponse.Content.ReadFromJsonAsync<Guid>();
        var ledger = (await Client.GetFromJsonAsync<LedgerShape>($"/api/ledgers/{ledgerId}"))!;

        var accountId = ledger.Accounts.Single().Id;
        var income = await Client.PostAsJsonAsync($"/api/accounts/{accountId}/transaction",
            new CreateAccountTransactionCommand(accountId, new(100, "EUR"), DateTimeOffset.UtcNow, "Salary"));
        income.StatusCode.ShouldBe(HttpStatusCode.Created, await income.Content.ReadAsStringAsync());

        return ledger;
    }
}
