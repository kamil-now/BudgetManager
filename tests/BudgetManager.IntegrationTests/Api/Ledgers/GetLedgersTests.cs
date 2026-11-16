using System.Net;
using System.Net.Http.Json;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Api.Ledgers;

public class GetLedgersTests(ITestOutputHelper testOutputHelper, ApiFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    private const string _baseUrl = "/api/ledgers";

    private sealed record LedgerItem(Guid Id, string Name,Dictionary<string, decimal> Balance);

    [Fact]
    public async Task GetLedgers_WhenUserIsUnauthorized_401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.GetAsync(_baseUrl);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetLedgers_WhenUserHasLedgers_200()
    {
        // Arrange
        await RegisterAndLogin();
        var otherResponse = await Client.PostAsJsonAsync(_baseUrl, LedgersControllerTests.ValidCommand);
        otherResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await otherResponse.Content.ReadAsStringAsync());
        await RegisterAndLogin();
        var createResponse = await Client.PostAsJsonAsync(_baseUrl, LedgersControllerTests.ValidCommand);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await createResponse.Content.ReadAsStringAsync());
        var ledgerId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        // Act
        var response = await Client.GetAsync(_baseUrl);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var ledgers = await response.Content.ReadFromJsonAsync<LedgerItem[]>();
        ledgers.ShouldNotBeNull();
        ledgers.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            x => x.Id.ShouldBe(ledgerId),
            x => x.Balance.ShouldHaveSingleItem().ShouldBe(new KeyValuePair<string, decimal>("EUR", 123)));
    }

    [Fact]
    public async Task GetLedgers_WhenUserHasNoLedgers_200()
    {
        // Arrange
        await RegisterAndLogin();

        // Act
        var response = await Client.GetAsync(_baseUrl);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var ledgers = await response.Content.ReadFromJsonAsync<LedgerItem[]>();
        ledgers.ShouldNotBeNull();
        ledgers.ShouldBeEmpty();
    }
}
