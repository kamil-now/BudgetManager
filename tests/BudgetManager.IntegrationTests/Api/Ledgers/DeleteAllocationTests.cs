using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudgetManager.Application.Commands;
using BudgetManager.Application.Models;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Api.Ledgers;

public class DeleteAllocationTests(ITestOutputHelper testOutputHelper, ApiFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    private sealed record LedgerShape(Guid Id, BudgetShape[] Budgets, AccountShape[] Accounts);
    private sealed record BudgetShape(Guid Id, FundShape[] Funds);
    private sealed record FundShape(Guid Id, string Name);
    private sealed record AccountShape(Guid Id);

    [Fact]
    public async Task DeleteAllocation_WhenUserIsUnauthorized_401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.DeleteAsync($"/api/ledgers/{Guid.NewGuid()}/allocations/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteAllocation_WhenIdIsAnAllocationGroup_204()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerWithIncomeAsync(allocate: false);
        var allocationId = await AllocateAsync(ledger);

        // Act
        var response = await Client.DeleteAsync($"/api/ledgers/{ledger.Id}/allocations/{allocationId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task DeleteAllocation_WhenAllocationBelongsToAnotherUser_403()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerWithIncomeAsync(allocate: false);
        var allocationId = await AllocateAsync(ledger);

        await RegisterAndLogin();

        // Act
        var response = await Client.DeleteAsync($"/api/ledgers/{ledger.Id}/allocations/{allocationId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteAllocation_WhenAllocationDoesNotExist_403()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerWithIncomeAsync(allocate: false);

        // Act
        var response = await Client.DeleteAsync($"/api/ledgers/{ledger.Id}/allocations/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteAllocation_WhenIdIsAnAllocationMadeFromIncome_403()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerWithIncomeAsync(allocate: true);
        var feed = await Client.GetFromJsonAsync<JsonElement>($"/api/ledgers/{ledger.Id}/feed");
        var allocationId = feed.GetProperty("items").EnumerateArray().Single()
            .GetProperty("allocations").EnumerateArray().Single().GetProperty("id").GetGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/ledgers/{ledger.Id}/allocations/{allocationId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteAllocation_WhenAllocationIsInitial_204()
    {
        // Arrange
        await RegisterAndLogin();
        var createResponse = await Client.PostAsJsonAsync("/api/ledgers", new CreateLedgerCommand(
            "[ledger name]",
            [new("[budget name]", [new CreateFundDTO("Food")])],
            [new(new(100, "EUR"), "[account name]")],
            [new InitialAllocationDTO("[budget name]", "Food", new(100, "EUR"))]));
        var ledgerId = await createResponse.Content.ReadFromJsonAsync<Guid>();
        var feed = await Client.GetFromJsonAsync<JsonElement>($"/api/ledgers/{ledgerId}/feed?types=4");
        var allocationId = feed.GetProperty("items").EnumerateArray().Single().GetProperty("id").GetGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/ledgers/{ledgerId}/allocations/{allocationId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
    }

    private static Guid Food(LedgerShape ledger) => ledger.Budgets.Single().Funds.Single(x => x.Name == "Food").Id;

    private static object Lines(Guid fundId) => new { lines = new[] { new { fundId, sequence = 0, type = 0, amount = 25m } } };

    private async Task<Guid> AllocateAsync(LedgerShape ledger)
    {
        var response = await Client.PostAsJsonAsync($"/api/ledgers/{ledger.Id}/allocations", new { currency = "EUR", allocation = Lines(Food(ledger)) });
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    /// <summary>
    /// A ledger whose only account got an income of 100 EUR, of which 25 are allocated to Food when <paramref name="allocate"/>.
    /// </summary>
    private async Task<LedgerShape> CreateLedgerWithIncomeAsync(bool allocate)
    {
        var createResponse = await Client.PostAsJsonAsync("/api/ledgers", new CreateLedgerCommand(
            "[ledger name]",
            [new("[budget name]", [new CreateFundDTO("Food")])],
            [new(new(0, "EUR"), "[account name]")]));
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await createResponse.Content.ReadAsStringAsync());
        var ledgerId = await createResponse.Content.ReadFromJsonAsync<Guid>();
        var ledger = (await Client.GetFromJsonAsync<LedgerShape>($"/api/ledgers/{ledgerId}"))!;

        var accountId = ledger.Accounts.Single().Id;
        var income = await Client.PostAsJsonAsync($"/api/accounts/{accountId}/transaction", new
        {
            accountId,
            value = new { amount = 100m, currency = "EUR" },
            date = DateTimeOffset.UtcNow,
            title = "Salary",
            allocation = allocate ? Lines(Food(ledger)) : null
        });
        income.StatusCode.ShouldBe(HttpStatusCode.Created, await income.Content.ReadAsStringAsync());

        return ledger;
    }
}
