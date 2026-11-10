using System.Net;
using System.Net.Http.Json;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Api.Budgets;

public class GetBudgetTests(ITestOutputHelper testOutputHelper, ApiFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    private sealed record LedgerShape(Guid Id, BudgetShape[] Budgets);
    private sealed record BudgetShape(Guid Id, string Name);

    [Fact]
    public async Task GetBudget_WhenUserIsUnauthorized_401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.GetAsync($"/api/budgets/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBudget_WhenBudgetBelongsToUser_200()
    {
        // Arrange
        await RegisterAndLogin();
        var budgetId = await CreateLedgerAndGetBudgetId();

        // Act
        var response = await Client.GetAsync($"/api/budgets/{budgetId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var budget = await response.Content.ReadFromJsonAsync<BudgetShape>();
        budget.ShouldNotBeNull();
        budget.Id.ShouldBe(budgetId);
    }

    [Fact]
    public async Task GetBudget_WhenBudgetBelongsToAnotherUser_403()
    {
        // Arrange
        await RegisterAndLogin();
        var budgetId = await CreateLedgerAndGetBudgetId();

        await RegisterAndLogin();

        // Act
        var response = await Client.GetAsync($"/api/budgets/{budgetId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetBudget_WhenResourceDoesNotExist_403()
    {
        // Arrange
        await RegisterAndLogin();

        // Act
        var response = await Client.GetAsync($"/api/budgets/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private async Task<Guid> CreateLedgerAndGetBudgetId()
    {
        var createResponse = await Client.PostAsJsonAsync("/api/ledgers", Ledgers.LedgersControllerTests.ValidCommand);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await createResponse.Content.ReadAsStringAsync());
        var ledgerId = await createResponse.Content.ReadFromJsonAsync<Guid>();
        var getResponse = await Client.GetAsync($"/api/ledgers/{ledgerId}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await getResponse.Content.ReadAsStringAsync());
        var ledger = await getResponse.Content.ReadFromJsonAsync<LedgerShape>();
        return ledger!.Budgets.First().Id;
    }
}
