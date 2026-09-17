using System.Net;
using System.Net.Http.Json;
using BudgetManager.Application.Commands;
using BudgetManager.Application.Models;
using BudgetManager.Domain.Enums;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Api.Budgets;

public class CreateBudgetTests(ITestOutputHelper testOutputHelper, ApiFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    private const string _baseUrl = "/api/budgets";

    private static readonly CreateFundDTO[] _funds = [new("[fund name]", 0, 100, AllocationType.Fixed, "[fund description]")];

    [Fact]
    public async Task CreateBudget_WhenUserIsUnauthorized_401()
    => await AssertPostFailsWhenUnauthorized(_baseUrl);

    [Fact]
    public async Task CreateBudget_WhenBearerTokenIsInvalid_401()
    => await AssertUnauthorizedWhenTokenIsInvalid(_baseUrl);

    [Fact]
    public async Task CreateBudget_WhenRequestIsValid_201()
    {
        // Arrange
        await RegisterAndLogin();
        var ledgerId = await CreateLedgerAsync();

        // Act
        var response = await Client.PostAsJsonAsync(_baseUrl, new CreateBudgetCommand(ledgerId, "[new budget name]", _funds, "[budget description]"));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateBudget_WhenFundsAreEmpty_422()
    {
        // Arrange
        await RegisterAndLogin();
        var ledgerId = await CreateLedgerAsync();

        // Act
        var response = await Client.PostAsJsonAsync(_baseUrl, new CreateBudgetCommand(ledgerId, "[new budget name]", []));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateBudget_WhenBudgetWithSameNameExistsInLedger_409()
    {
        // Arrange
        await RegisterAndLogin();
        var ledgerId = await CreateLedgerAsync();
        var command = new CreateBudgetCommand(ledgerId, "[new budget name]", _funds);
        await Client.PostAsJsonAsync(_baseUrl, command);

        // Act
        var response = await Client.PostAsJsonAsync(_baseUrl, command);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateBudget_WhenLedgerBelongsToAnotherUser_403()
    {
        // Arrange
        await RegisterAndLogin();
        var ledgerId = await CreateLedgerAsync();

        await RegisterAndLogin();

        // Act
        var response = await Client.PostAsJsonAsync(_baseUrl, new CreateBudgetCommand(ledgerId, "[new budget name]", _funds));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private async Task<Guid> CreateLedgerAsync()
    {
        var response = await Client.PostAsJsonAsync("/api/ledgers", Ledgers.LedgersControllerTests.ValidCommand);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.Content.ReadFromJsonAsync<Guid>();
    }
}
