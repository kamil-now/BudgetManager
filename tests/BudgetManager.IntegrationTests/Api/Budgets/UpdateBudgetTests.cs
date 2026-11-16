using System.Net;
using System.Net.Http.Json;
using BudgetManager.Application.Commands;
using BudgetManager.Application.Models;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Api.Budgets;

public class UpdateBudgetTests(ITestOutputHelper testOutputHelper, ApiFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    private sealed record LedgerShape(Guid Id, BudgetShape[] Budgets);
    private sealed record BudgetShape(Guid Id, string Name, FundShape[] Funds);
    private sealed record FundShape(Guid Id, string Name);

    [Fact]
    public async Task UpdateBudget_WhenUserIsUnauthorized_401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.PutAsJsonAsync($"/api/budgets/{Guid.NewGuid()}", new { name = "Home", funds = Array.Empty<object>() });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateBudget_WhenFundsAreRenamedAddedAndRemoved_204()
    {
        // Arrange
        await RegisterAndLogin();
        var budget = await CreateBudgetAsync();
        var food = budget.Funds.Single(x => x.Name == "Food").Id;

        // Act
        var response = await Client.PutAsJsonAsync($"/api/budgets/{budget.Id}", new
        {
            name = "Home",
            funds = new object[] { new { id = food, name = "Groceries" }, new { name = "Repairs" } }
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
        var updated = await Client.GetFromJsonAsync<BudgetShape>($"/api/budgets/{budget.Id}");
        updated.ShouldNotBeNull().Name.ShouldBe("Home");
        updated.Funds.Select(x => x.Name).Order().ShouldBe(["Groceries", "Repairs"]);
        updated.Funds.Single(x => x.Name == "Groceries").Id.ShouldBe(food);
    }

    [Fact]
    public async Task UpdateBudget_WhenFundsAreEmpty_422()
    {
        // Arrange
        await RegisterAndLogin();
        var budget = await CreateBudgetAsync();

        // Act
        var response = await Client.PutAsJsonAsync($"/api/budgets/{budget.Id}", new { name = "Home", funds = Array.Empty<object>() });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateBudget_WhenNameIsTaken_409()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerAsync();
        var post = await Client.PostAsJsonAsync("/api/budgets", new CreateBudgetCommand(ledger.Id, "Other", [new("Travel")]));
        var otherId = await post.Content.ReadFromJsonAsync<Guid>();

        // Act
        var response = await Client.PutAsJsonAsync($"/api/budgets/{otherId}", new { name = "[budget name]", funds = new[] { new { name = "Travel" } } });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateBudget_WhenBudgetBelongsToAnotherUser_403()
    {
        // Arrange
        await RegisterAndLogin();
        var budget = await CreateBudgetAsync();

        await RegisterAndLogin();

        // Act
        var response = await Client.PutAsJsonAsync($"/api/budgets/{budget.Id}", new { name = "Home", funds = new[] { new { name = "Food" } } });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private async Task<BudgetShape> CreateBudgetAsync() => (await CreateLedgerAsync()).Budgets.Single();

    private async Task<LedgerShape> CreateLedgerAsync()
    {
        var createResponse = await Client.PostAsJsonAsync("/api/ledgers", new CreateLedgerCommand(
            "[ledger name]",
            [new("[budget name]", [new CreateFundDTO("Food"), new CreateFundDTO("Rent")])],
            [new(new(0, "EUR"), "[account name]")]));
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await createResponse.Content.ReadAsStringAsync());
        var ledgerId = await createResponse.Content.ReadFromJsonAsync<Guid>();
        return (await Client.GetFromJsonAsync<LedgerShape>($"/api/ledgers/{ledgerId}"))!;
    }
}
