using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudgetManager.Application.Commands;
using BudgetManager.Application.Models;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Api.Ledgers;

public class LedgerAllocationPreviewTests(ITestOutputHelper testOutputHelper, ApiFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    private sealed record LedgerShape(Guid Id, BudgetShape[] Budgets);
    private sealed record BudgetShape(Guid Id, FundShape[] Funds);
    private sealed record FundShape(Guid Id, string Name);

    private static string Url(Guid ledgerId) => $"/api/ledgers/{ledgerId}/allocation-preview";

    private static object Body(Guid fundId, decimal amount = 100) => new
    {
        amount,
        currency = "EUR",
        allocation = new { lines = new object[] { new { fundId, sequence = 0, type = 0, amount = 30m }, new { fundId, sequence = 1, type = 1, percent = 50m } } }
    };

    [Fact]
    public async Task GetLedgerAllocationPreview_WhenUserIsUnauthorized_401()
        => await AssertPostFailsWhenUnauthorized(Url(Guid.NewGuid()));

    [Fact]
    public async Task GetLedgerAllocationPreview_WhenLinesAreValid_200_WithALinePerRule()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerAsync();
        var food = ledger.Budgets.Single().Funds.Single().Id;

        // Act
        var response = await Client.PostAsJsonAsync(Url(ledger.Id), Body(food));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var lines = json.RootElement.GetProperty("lines").EnumerateArray().ToArray();
        lines.Select(x => (x.GetProperty("fundId").GetGuid(), x.GetProperty("fundName").GetString(), x.GetProperty("budgetName").GetString(), x.GetProperty("amount").GetDecimal()))
            .ShouldBe([(food, "Food", "[budget name]", 30m), (food, "Food", "[budget name]", 35m)]);
        json.RootElement.GetProperty("unallocated").GetDecimal().ShouldBe(35);
    }

    [Fact]
    public async Task GetLedgerAllocationPreview_WhenAmountIsNotPositive_422()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerAsync();

        // Act
        var response = await Client.PostAsJsonAsync(Url(ledger.Id), Body(ledger.Budgets.Single().Funds.Single().Id, amount: 0));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetLedgerAllocationPreview_WhenLedgerBelongsToAnotherUser_403()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerAsync();

        await RegisterAndLogin();

        // Act
        var response = await Client.PostAsJsonAsync(Url(ledger.Id), Body(ledger.Budgets.Single().Funds.Single().Id));

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
