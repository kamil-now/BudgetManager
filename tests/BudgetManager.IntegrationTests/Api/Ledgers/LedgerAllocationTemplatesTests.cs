using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudgetManager.Application.Commands;
using BudgetManager.Application.Models;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Api.Ledgers;

public class LedgerAllocationTemplatesTests(ITestOutputHelper testOutputHelper, ApiFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    private sealed record LedgerShape(Guid Id, BudgetShape[] Budgets);
    private sealed record BudgetShape(Guid Id, string Name);

    [Fact]
    public async Task GetLedgerAllocationTemplates_WhenUserIsUnauthorized_401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.GetAsync($"/api/ledgers/{Guid.NewGuid()}/allocationTemplates");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateLedgerAllocationTemplate_WhenUserIsUnauthorized_401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.PostAsJsonAsync($"/api/ledgers/{Guid.NewGuid()}/allocationTemplates", new { name = "A", currency = "EUR", lines = Array.Empty<object>() });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateLedgerAllocationTemplate_WhenUserIsUnauthorized_401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.PutAsJsonAsync($"/api/ledgers/{Guid.NewGuid()}/allocationTemplates/{Guid.NewGuid()}", new { name = "A", lines = Array.Empty<object>() });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteLedgerAllocationTemplate_WhenUserIsUnauthorized_401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.DeleteAsync($"/api/ledgers/{Guid.NewGuid()}/allocationTemplates/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LedgerAllocationTemplates_WhenTemplateIsCreatedReadUpdatedAndDeleted_201_200_204_204()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerAsync();
        var home = ledger.Budgets.Single(x => x.Name == "Home").Id;
        var travel = ledger.Budgets.Single(x => x.Name == "Travel").Id;

        // Act
        var post = await Client.PostAsJsonAsync($"/api/ledgers/{ledger.Id}/allocationTemplates", new
        {
            name = "Salary",
            currency = "EUR",
            lines = new object[]
            {
                new { budgetId = home, sequence = 0, type = 0, amount = 500 },
                new { budgetId = travel, sequence = 1, type = 1, percent = 100 },
            }
        });
        var id = await post.Content.ReadFromJsonAsync<Guid>();
        var get = await Client.GetAsync($"/api/ledgers/{ledger.Id}/allocationTemplates");
        var put = await Client.PutAsJsonAsync($"/api/ledgers/{ledger.Id}/allocationTemplates/{id}", new
        {
            name = "Renamed",
            lines = new object[] { new { budgetId = travel, sequence = 0, type = 1, percent = 100 } }
        });
        var getAfterUpdate = await Client.GetFromJsonAsync<LedgerAllocationTemplateDTO[]>($"/api/ledgers/{ledger.Id}/allocationTemplates");
        var delete = await Client.DeleteAsync($"/api/ledgers/{ledger.Id}/allocationTemplates/{id}");
        var getAfterDelete = await Client.GetAsync($"/api/ledgers/{ledger.Id}/allocationTemplates");

        // Assert
        post.StatusCode.ShouldBe(HttpStatusCode.Created, await post.Content.ReadAsStringAsync());
        get.StatusCode.ShouldBe(HttpStatusCode.OK, await get.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        var template = json.RootElement.EnumerateArray().ShouldHaveSingleItem();
        template.GetProperty("id").GetGuid().ShouldBe(id);
        template.GetProperty("name").GetString().ShouldBe("Salary");
        template.GetProperty("currency").GetString().ShouldBe("EUR");
        template.GetProperty("minimumAmount").GetDecimal().ShouldBe(500);
        var lines = template.GetProperty("lines").EnumerateArray().ToArray();
        lines.Length.ShouldBe(2);
        lines[0].GetProperty("budgetId").GetGuid().ShouldBe(home);
        lines[0].GetProperty("budgetName").GetString().ShouldBe("Home");
        lines[0].GetProperty("type").GetInt32().ShouldBe(0);
        lines[0].GetProperty("amount").GetDecimal().ShouldBe(500);
        lines[0].GetProperty("percent").ValueKind.ShouldBe(JsonValueKind.Null);
        lines[1].GetProperty("percent").GetDecimal().ShouldBe(100);

        put.StatusCode.ShouldBe(HttpStatusCode.NoContent, await put.Content.ReadAsStringAsync());
        var updated = getAfterUpdate.ShouldNotBeNull().ShouldHaveSingleItem();
        updated.Name.ShouldBe("Renamed");
        updated.Lines.ShouldHaveSingleItem().BudgetId.ShouldBe(travel);

        delete.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await getAfterDelete.Content.ReadAsStringAsync()).ShouldBe("[]");
    }

    [Fact]
    public async Task CreateLedgerAllocationTemplate_WhenLinesAreEmpty_422()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerAsync();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/ledgers/{ledger.Id}/allocationTemplates",
            new { name = "Salary", currency = "EUR", lines = Array.Empty<object>() });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadAsStringAsync()).ShouldBe("Allocation lines cannot be empty.");
    }

    [Fact]
    public async Task CreateLedgerAllocationTemplate_WhenNameIsTaken_409()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerAsync();
        var body = new { name = "Salary", currency = "EUR", lines = new[] { new { budgetId = ledger.Budgets[0].Id, sequence = 0, type = 1, percent = 10 } } };
        await Client.PostAsJsonAsync($"/api/ledgers/{ledger.Id}/allocationTemplates", body);

        // Act
        var response = await Client.PostAsJsonAsync($"/api/ledgers/{ledger.Id}/allocationTemplates", body);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateLedgerAllocationTemplate_WhenLedgerBelongsToAnotherUser_403()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerAsync();

        await RegisterAndLogin();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/ledgers/{ledger.Id}/allocationTemplates",
            new { name = "Salary", currency = "EUR", lines = new[] { new { budgetId = ledger.Budgets[0].Id, sequence = 0, type = 1, percent = 10 } } });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetLedgerAllocationTemplates_WhenLedgerBelongsToAnotherUser_403()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerAsync();

        await RegisterAndLogin();

        // Act
        var response = await Client.GetAsync($"/api/ledgers/{ledger.Id}/allocationTemplates");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateLedgerAllocationTemplate_WhenTemplateDoesNotExist_404()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerAsync();

        // Act
        var response = await Client.PutAsJsonAsync($"/api/ledgers/{ledger.Id}/allocationTemplates/{Guid.NewGuid()}",
            new { name = "Salary", lines = new[] { new { budgetId = ledger.Budgets[0].Id, sequence = 0, type = 1, percent = 10 } } });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteLedgerAllocationTemplate_WhenTemplateDoesNotExist_404()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerAsync();

        // Act
        var response = await Client.DeleteAsync($"/api/ledgers/{ledger.Id}/allocationTemplates/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteLedgerAllocationTemplate_WhenLedgerBelongsToAnotherUser_403()
    {
        // Arrange
        await RegisterAndLogin();
        var ledger = await CreateLedgerAsync();

        await RegisterAndLogin();

        // Act
        var response = await Client.DeleteAsync($"/api/ledgers/{ledger.Id}/allocationTemplates/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private async Task<LedgerShape> CreateLedgerAsync()
    {
        var createResponse = await Client.PostAsJsonAsync("/api/ledgers", new CreateLedgerCommand(
            "[ledger name]",
            [
                new("Home", [new CreateFundDTO("Food"), new CreateFundDTO("Rent")]),
                new("Travel", [new CreateFundDTO("Flights")])
            ],
            [new(new(0, "EUR"), "[account name]")]));
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await createResponse.Content.ReadAsStringAsync());
        var ledgerId = await createResponse.Content.ReadFromJsonAsync<Guid>();
        return (await Client.GetFromJsonAsync<LedgerShape>($"/api/ledgers/{ledgerId}"))!;
    }
}
