using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudgetManager.Application.Commands;
using BudgetManager.Application.Models;
using BudgetManager.Domain.Enums;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Api.Accounts;

public class CreateTransactionTests(ITestOutputHelper testOutputHelper, ApiFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    private sealed record LedgerShape(Guid Id, BudgetShape[] Budgets, AccountShape[] Accounts);
    private sealed record BudgetShape(Guid Id, FundShape[] Funds);
    private sealed record FundShape(Guid Id, string Name);
    private sealed record AccountShape(Guid Id);

    [Fact]
    public async Task CreateTransaction_WhenUserIsUnauthorized_401()
        => await AssertPostFailsWhenUnauthorized($"/api/accounts/{Guid.NewGuid()}/transaction");

    [Fact]
    public async Task CreateTransaction_WhenIncomeIsAllocatedByLines_201_AndTransactionListsAllocations()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerAsync();
        var accountId = ledger.Accounts.Single().Id;
        var food = FundId(ledger, "Food");

        // Act
        var response = await Client.PostAsJsonAsync($"/api/accounts/{accountId}/transaction", new
        {
            accountId,
            value = new { amount = 100, currency = "EUR" },
            date = DateTimeOffset.UtcNow,
            title = "Salary",
            allocation = new { lines = new[] { new { fundId = food, sequence = 0, type = 0, amount = 40 } } }
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var id = await response.Content.ReadFromJsonAsync<Guid>();

        var get = await Client.GetAsync($"/api/accounts/{accountId}/transaction/{id}");
        get.StatusCode.ShouldBe(HttpStatusCode.OK, await get.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        var allocation = json.RootElement.GetProperty("allocations").EnumerateArray().ShouldHaveSingleItem();
        allocation.GetProperty("fundId").GetGuid().ShouldBe(food);
        allocation.GetProperty("amount").GetDecimal().ShouldBe(40);
    }

    [Fact]
    public async Task CreateTransaction_WhenIncomeIsAllocatedByTemplate_201()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerAsync();
        var accountId = ledger.Accounts.Single().Id;
        var budgetId = ledger.Budgets.Single().Id;
        var post = await Client.PostAsJsonAsync($"/api/budgets/{budgetId}/allocationTemplates", new
        {
            name = "Salary",
            currency = "EUR",
            lines = new[] { new { fundId = FundId(ledger, "Food"), sequence = 0, type = 1, percent = 10 } }
        });
        post.StatusCode.ShouldBe(HttpStatusCode.Created, await post.Content.ReadAsStringAsync());
        var templateId = await post.Content.ReadFromJsonAsync<Guid>();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/accounts/{accountId}/transaction",
            new CreateAccountTransactionCommand(accountId, new(250, "EUR"), DateTimeOffset.UtcNow, "Salary", Allocation: new AllocationInput(TemplateId: templateId)));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CreateTransaction_WhenAllocationPercentExceeds100_422()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerAsync();
        var accountId = ledger.Accounts.Single().Id;

        // Act
        var response = await Client.PostAsJsonAsync($"/api/accounts/{accountId}/transaction",
            new CreateAccountTransactionCommand(accountId, new(100, "EUR"), DateTimeOffset.UtcNow, "Salary",
                Allocation: new AllocationInput(Lines: [
                    new(FundId(ledger, "Food"), 0, AllocationType.Percent, Percent: 60),
                    new(FundId(ledger, "Food"), 1, AllocationType.Percent, Percent: 100.01m)])));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateTransaction_WhenFundBelongsToAnotherUser_403()
    {
        // Arrange
        await RegisterAndLogin();
        var otherUsersLedger = await CreateLedgerAsync();

        await RegisterAndLogin();
        var ledger = await CreateLedgerAsync();
        var accountId = ledger.Accounts.Single().Id;

        // Act
        var response = await Client.PostAsJsonAsync($"/api/accounts/{accountId}/transaction",
            new CreateAccountTransactionCommand(accountId, new(100, "EUR"), DateTimeOffset.UtcNow, "Salary",
                Allocation: new AllocationInput(Lines: [new(FundId(otherUsersLedger, "Food"), 0, AllocationType.Fixed, 10)])));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateTransaction_WhenAllocationLinesAreEmpty_200_AndAllocationsAreCleared()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerAsync();
        var accountId = ledger.Accounts.Single().Id;
        var create = await Client.PostAsJsonAsync($"/api/accounts/{accountId}/transaction",
            new CreateAccountTransactionCommand(accountId, new(100, "EUR"), DateTimeOffset.UtcNow, "Salary",
                Allocation: new AllocationInput(Lines: [new(FundId(ledger, "Food"), 0, AllocationType.Fixed, 10)])));
        create.StatusCode.ShouldBe(HttpStatusCode.Created, await create.Content.ReadAsStringAsync());
        var id = await create.Content.ReadFromJsonAsync<Guid>();

        // Act
        var response = await Client.PutAsJsonAsync($"/api/accounts/{accountId}/transaction/{id}", new
        {
            id,
            accountId,
            amount = new { amount = 100, currency = "EUR" },
            date = DateTimeOffset.UtcNow,
            title = "Salary",
            allocation = new { lines = Array.Empty<object>() }
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var transaction = await Client.GetFromJsonAsync<AccountTransactionDTO>($"/api/accounts/{accountId}/transaction/{id}");
        transaction.ShouldNotBeNull().Allocations.ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateTransaction_WhenExpenseIsPaidFromFund_201_AndTransactionReturnsFundId()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerAsync();
        var accountId = ledger.Accounts.Single().Id;
        var food = FundId(ledger, "Food");

        // Act
        var response = await Client.PostAsJsonAsync($"/api/accounts/{accountId}/transaction",
            new CreateAccountTransactionCommand(accountId, new(-30, "EUR"), DateTimeOffset.UtcNow, "Groceries", FundId: food));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        var transaction = await Client.GetFromJsonAsync<AccountTransactionDTO>($"/api/accounts/{accountId}/transaction/{id}");
        transaction.ShouldNotBeNull().FundId.ShouldBe(food);
    }

    [Fact]
    public async Task CreateTransaction_WhenIncomeIsPaidFromFund_422()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerAsync();
        var accountId = ledger.Accounts.Single().Id;

        // Act
        var response = await Client.PostAsJsonAsync($"/api/accounts/{accountId}/transaction",
            new CreateAccountTransactionCommand(accountId, new(30, "EUR"), DateTimeOffset.UtcNow, "Salary", FundId: FundId(ledger, "Food")));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateTransaction_WhenPayingFundBelongsToAnotherUser_403()
    {
        // Arrange
        await RegisterAndLogin();
        var otherUsersLedger = await CreateLedgerAsync();

        await RegisterAndLogin();
        var ledger = await CreateLedgerAsync();
        var accountId = ledger.Accounts.Single().Id;

        // Act
        var response = await Client.PostAsJsonAsync($"/api/accounts/{accountId}/transaction",
            new CreateAccountTransactionCommand(accountId, new(-30, "EUR"), DateTimeOffset.UtcNow, "Groceries", FundId: FundId(otherUsersLedger, "Food")));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private static Guid FundId(LedgerShape ledger, string name)
        => ledger.Budgets.SelectMany(x => x.Funds).Single(x => x.Name == name).Id;

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
