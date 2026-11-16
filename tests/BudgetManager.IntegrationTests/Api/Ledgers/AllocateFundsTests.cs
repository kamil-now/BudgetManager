using System.Net;
using System.Net.Http.Json;
using BudgetManager.Application.Commands;
using BudgetManager.Application.Models;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Api.Ledgers;

public class AllocateFundsTests(ITestOutputHelper testOutputHelper, ApiFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    private sealed record LedgerShape(Guid Id, BudgetShape[] Budgets, AccountShape[] Accounts);
    private sealed record BudgetShape(Guid Id, FundShape[] Funds);
    private sealed record FundShape(Guid Id, string Name);
    private sealed record AccountShape(Guid Id);

    private static string Url(Guid ledgerId) => $"/api/ledgers/{ledgerId}/allocations";

    private static object Body(Guid fundId, decimal? amount = null) => new
    {
        currency = "EUR",
        allocation = new { lines = new[] { new { fundId, sequence = 0, type = 0, amount = 25m } } },
        amount
    };

    [Fact]
    public async Task AllocateFunds_WhenUserIsUnauthorized_401()
        => await AssertPostFailsWhenUnauthorized(Url(Guid.NewGuid()));

    [Fact]
    public async Task AllocateFunds_WhenBearerTokenIsInvalid_401()
        => await AssertUnauthorizedWhenTokenIsInvalid(Url(Guid.NewGuid()));

    [Fact]
    public async Task AllocateFunds_WhenLedgerBelongsToAnotherUser_403()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerWithIncomeAsync();

        await RegisterAndLogin();

        // Act
        var response = await Client.PostAsJsonAsync(Url(ledger.Id), Body(Food(ledger)));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AllocateFunds_WhenRequestIsValid_200_WithAllocationId()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerWithIncomeAsync();

        // Act
        var response = await Client.PostAsJsonAsync(Url(ledger.Id), Body(Food(ledger)));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await response.Content.ReadFromJsonAsync<Guid>()).ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task AllocateFunds_WhenAmountExceedsUnallocated_422()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerWithIncomeAsync();

        // Act
        var response = await Client.PostAsJsonAsync(Url(ledger.Id), Body(Food(ledger), amount: 100.01m));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    private static Guid Food(LedgerShape ledger) => ledger.Budgets.Single().Funds.Single(x => x.Name == "Food").Id;

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
