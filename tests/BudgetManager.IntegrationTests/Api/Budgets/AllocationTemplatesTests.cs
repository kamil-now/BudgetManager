using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudgetManager.Application.Commands;
using BudgetManager.Application.Models;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Api.Budgets;

public class AllocationTemplatesTests(ITestOutputHelper testOutputHelper, ApiFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    private sealed record LedgerShape(Guid Id, BudgetShape[] Budgets);
    private sealed record BudgetShape(Guid Id, FundShape[] Funds);
    private sealed record FundShape(Guid Id, string Name);

    [Fact]
    public async Task GetAllocationTemplates_WhenUserIsUnauthorized_401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.GetAsync($"/api/budgets/{Guid.NewGuid()}/allocationTemplates");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateAllocationTemplate_WhenUserIsUnauthorized_401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.PostAsJsonAsync($"/api/budgets/{Guid.NewGuid()}/allocationTemplates", new { name = "A", currency = "EUR", lines = Array.Empty<object>() });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateAllocationTemplate_WhenUserIsUnauthorized_401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.PutAsJsonAsync($"/api/budgets/{Guid.NewGuid()}/allocationTemplates/{Guid.NewGuid()}", new { name = "A", lines = Array.Empty<object>() });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteAllocationTemplate_WhenUserIsUnauthorized_401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.DeleteAsync($"/api/budgets/{Guid.NewGuid()}/allocationTemplates/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAllocationTemplates_WhenBudgetHasNoTemplates_200_WithEmptyArray()
    {
        // Arrange
        await RegisterAndLogin();
        var budget = await CreateBudgetAsync();

        // Act
        var response = await Client.GetAsync($"/api/budgets/{budget.Id}/allocationTemplates");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await response.Content.ReadAsStringAsync()).ShouldBe("[]");
    }

    [Fact]
    public async Task AllocationTemplates_WhenTemplateIsCreatedReadUpdatedAndDeleted_201_200_204_204()
    {
        // Arrange
        await RegisterAndLogin();
        var budget = await CreateBudgetAsync();
        var food = budget.Funds.Single(x => x.Name == "Food").Id;
        var rent = budget.Funds.Single(x => x.Name == "Rent").Id;

        // Act
        var post = await Client.PostAsJsonAsync($"/api/budgets/{budget.Id}/allocationTemplates", new
        {
            name = "Salary",
            currency = "EUR",
            lines = new object[]
            {
                new { fundId = rent, sequence = 0, type = 1, percent = 50 },
                new { fundId = food, sequence = 1, type = 0, amount = 100 },
            }
        });
        var id = await post.Content.ReadFromJsonAsync<Guid>();
        var get = await Client.GetAsync($"/api/budgets/{budget.Id}/allocationTemplates");
        var put = await Client.PutAsJsonAsync($"/api/budgets/{budget.Id}/allocationTemplates/{id}", new
        {
            name = "Renamed",
            lines = new object[] { new { fundId = food, sequence = 0, type = 1, percent = 100 } }
        });
        var getAfterUpdate = await Client.GetFromJsonAsync<AllocationTemplateDTO[]>($"/api/budgets/{budget.Id}/allocationTemplates");
        var delete = await Client.DeleteAsync($"/api/budgets/{budget.Id}/allocationTemplates/{id}");
        var getAfterDelete = await Client.GetAsync($"/api/budgets/{budget.Id}/allocationTemplates");

        // Assert
        post.StatusCode.ShouldBe(HttpStatusCode.Created, await post.Content.ReadAsStringAsync());
        get.StatusCode.ShouldBe(HttpStatusCode.OK, await get.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        var template = json.RootElement.EnumerateArray().ShouldHaveSingleItem();
        template.GetProperty("id").GetGuid().ShouldBe(id);
        template.GetProperty("name").GetString().ShouldBe("Salary");
        template.GetProperty("currency").GetString().ShouldBe("EUR");
        template.GetProperty("minimumAmount").GetDecimal().ShouldBe(199.99m);
        var lines = template.GetProperty("lines").EnumerateArray().ToArray();
        lines.Length.ShouldBe(2);
        lines[0].GetProperty("fundId").GetGuid().ShouldBe(rent);
        lines[0].GetProperty("fundName").GetString().ShouldBe("Rent");
        lines[0].GetProperty("sequence").GetInt32().ShouldBe(0);
        lines[0].GetProperty("type").GetInt32().ShouldBe(1);
        lines[0].GetProperty("percent").GetDecimal().ShouldBe(50);
        lines[0].GetProperty("amount").ValueKind.ShouldBe(JsonValueKind.Null);
        lines[1].GetProperty("type").GetInt32().ShouldBe(0);
        lines[1].GetProperty("amount").GetDecimal().ShouldBe(100);

        put.StatusCode.ShouldBe(HttpStatusCode.NoContent, await put.Content.ReadAsStringAsync());
        var updated = getAfterUpdate.ShouldNotBeNull().ShouldHaveSingleItem();
        updated.Name.ShouldBe("Renamed");
        updated.Currency.ShouldBe("EUR");
        updated.Lines.ShouldHaveSingleItem();

        delete.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await getAfterDelete.Content.ReadAsStringAsync()).ShouldBe("[]");
    }

    [Fact]
    public async Task CreateAllocationTemplate_WhenRouteDiffersFromBody_ShouldUseRoute()
    {
        // Arrange
        await RegisterAndLogin();
        var budget = await CreateBudgetAsync();
        var food = budget.Funds.Single(x => x.Name == "Food").Id;

        // Act
        var post = await Client.PostAsJsonAsync($"/api/budgets/{budget.Id}/allocationTemplates",
            new CreateAllocationTemplateCommand(Guid.NewGuid(), "Salary", "USD", [new(food, 0, BudgetManager.Domain.Enums.AllocationType.Fixed, Amount: 10)]));

        // Assert
        post.StatusCode.ShouldBe(HttpStatusCode.Created, await post.Content.ReadAsStringAsync());
        var templates = await Client.GetFromJsonAsync<AllocationTemplateDTO[]>($"/api/budgets/{budget.Id}/allocationTemplates");
        templates.ShouldNotBeNull().ShouldHaveSingleItem().Currency.ShouldBe("USD");
    }

    [Fact]
    public async Task CreateAllocationTemplate_WhenBudgetBelongsToAnotherUser_403()
    {
        // Arrange
        await RegisterAndLogin();
        var budget = await CreateBudgetAsync();
        var food = budget.Funds.Single(x => x.Name == "Food").Id;

        await RegisterAndLogin();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/budgets/{budget.Id}/allocationTemplates",
            new { name = "Salary", currency = "EUR", lines = new[] { new { fundId = food, sequence = 0, type = 0, amount = 100 } } });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAllocationTemplates_WhenBudgetBelongsToAnotherUser_403()
    {
        // Arrange
        await RegisterAndLogin();
        var budget = await CreateBudgetAsync();

        await RegisterAndLogin();

        // Act
        var response = await Client.GetAsync($"/api/budgets/{budget.Id}/allocationTemplates");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateAllocationTemplate_WhenLinesAreEmpty_422()
    {
        // Arrange
        await RegisterAndLogin();
        var budget = await CreateBudgetAsync();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/budgets/{budget.Id}/allocationTemplates",
            new { name = "Salary", currency = "EUR", lines = Array.Empty<object>() });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadAsStringAsync()).ShouldBe("Allocation lines cannot be empty.");
    }

    [Fact]
    public async Task CreateAllocationTemplate_WhenHundredPercentStepIsNotLast_422()
    {
        // Arrange
        await RegisterAndLogin();
        var budget = await CreateBudgetAsync();
        var food = budget.Funds.Single(x => x.Name == "Food").Id;
        var rent = budget.Funds.Single(x => x.Name == "Rent").Id;

        // Act
        var response = await Client.PostAsJsonAsync($"/api/budgets/{budget.Id}/allocationTemplates", new
        {
            name = "Salary",
            currency = "EUR",
            lines = new[]
            {
                new { fundId = food, sequence = 0, type = 1, percent = 100 },
                new { fundId = rent, sequence = 1, type = 1, percent = 10 },
            }
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateAllocationTemplate_WhenNameIsTaken_409()
    {
        // Arrange
        await RegisterAndLogin();
        var budget = await CreateBudgetAsync();
        var food = budget.Funds.Single(x => x.Name == "Food").Id;
        var body = new { name = "Salary", currency = "EUR", lines = new[] { new { fundId = food, sequence = 0, type = 1, percent = 10 } } };
        await Client.PostAsJsonAsync($"/api/budgets/{budget.Id}/allocationTemplates", body);

        // Act
        var response = await Client.PostAsJsonAsync($"/api/budgets/{budget.Id}/allocationTemplates", body);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateAllocationTemplate_WhenTemplateDoesNotExist_404()
    {
        // Arrange
        await RegisterAndLogin();
        var budget = await CreateBudgetAsync();
        var food = budget.Funds.Single(x => x.Name == "Food").Id;

        // Act
        var response = await Client.PutAsJsonAsync($"/api/budgets/{budget.Id}/allocationTemplates/{Guid.NewGuid()}",
            new { name = "Salary", lines = new[] { new { fundId = food, sequence = 0, type = 1, percent = 10 } } });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateAllocationTemplate_WhenNameIsTakenByAnotherTemplate_409()
    {
        // Arrange
        await RegisterAndLogin();
        var budget = await CreateBudgetAsync();
        var food = budget.Funds.Single(x => x.Name == "Food").Id;
        object[] lines = [new { fundId = food, sequence = 0, type = 1, percent = 10 }];
        await Client.PostAsJsonAsync($"/api/budgets/{budget.Id}/allocationTemplates", new { name = "Taken", currency = "EUR", lines });
        var post = await Client.PostAsJsonAsync($"/api/budgets/{budget.Id}/allocationTemplates", new { name = "Other", currency = "EUR", lines });
        var id = await post.Content.ReadFromJsonAsync<Guid>();

        // Act
        var response = await Client.PutAsJsonAsync($"/api/budgets/{budget.Id}/allocationTemplates/{id}", new { name = "Taken", lines });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateAllocationTemplate_WhenStepsAreInvalid_422()
    {
        // Arrange
        await RegisterAndLogin();
        var budget = await CreateBudgetAsync();
        var food = budget.Funds.Single(x => x.Name == "Food").Id;
        var post = await Client.PostAsJsonAsync($"/api/budgets/{budget.Id}/allocationTemplates",
            new { name = "Salary", currency = "EUR", lines = new[] { new { fundId = food, sequence = 0, type = 1, percent = 10 } } });
        var id = await post.Content.ReadFromJsonAsync<Guid>();

        // Act
        var response = await Client.PutAsJsonAsync($"/api/budgets/{budget.Id}/allocationTemplates/{id}",
            new { name = "Salary", lines = new[] { new { fundId = food, sequence = 0, type = 1, percent = 100.01 } } });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateAllocationTemplate_WhenBudgetBelongsToAnotherUser_403()
    {
        // Arrange
        await RegisterAndLogin();
        var budget = await CreateBudgetAsync();
        var food = budget.Funds.Single(x => x.Name == "Food").Id;
        object[] lines = [new { fundId = food, sequence = 0, type = 1, percent = 10 }];
        var post = await Client.PostAsJsonAsync($"/api/budgets/{budget.Id}/allocationTemplates", new { name = "Salary", currency = "EUR", lines });
        var id = await post.Content.ReadFromJsonAsync<Guid>();

        await RegisterAndLogin();

        // Act
        var response = await Client.PutAsJsonAsync($"/api/budgets/{budget.Id}/allocationTemplates/{id}", new { name = "Salary", lines });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteAllocationTemplate_WhenTemplateDoesNotExist_404()
    {
        // Arrange
        await RegisterAndLogin();
        var budget = await CreateBudgetAsync();

        // Act
        var response = await Client.DeleteAsync($"/api/budgets/{budget.Id}/allocationTemplates/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteAllocationTemplate_WhenBudgetBelongsToAnotherUser_403()
    {
        // Arrange
        await RegisterAndLogin();
        var budget = await CreateBudgetAsync();

        await RegisterAndLogin();

        // Act
        var response = await Client.DeleteAsync($"/api/budgets/{budget.Id}/allocationTemplates/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private async Task<BudgetShape> CreateBudgetAsync()
    {
        var createResponse = await Client.PostAsJsonAsync("/api/ledgers", new CreateLedgerCommand(
            "[ledger name]",
            [new("[budget name]", [new CreateFundDTO("Food"), new CreateFundDTO("Rent")])],
            [new(new(0, "EUR"), "[account name]")]));
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await createResponse.Content.ReadAsStringAsync());
        var ledgerId = await createResponse.Content.ReadFromJsonAsync<Guid>();
        var ledger = await Client.GetFromJsonAsync<LedgerShape>($"/api/ledgers/{ledgerId}");
        return ledger!.Budgets.Single();
    }
}
