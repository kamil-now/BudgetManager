using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudgetManager.Application.Commands;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Api.Ledgers;

public class GetLedgerStatisticsTests(ITestOutputHelper testOutputHelper, ApiFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    private static string Url(Guid ledgerId, string query = "") => $"/api/ledgers/{ledgerId}/statistics{query}";

    [Fact]
    public async Task GetLedgerStatistics_WhenUserIsUnauthorized_401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.GetAsync(Url(Guid.NewGuid()));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetLedgerStatistics_WhenLedgerBelongsToAnotherUser_403()
    {
        // Arrange
        await RegisterAndLogin();
        var ledgerId = await CreateLedgerAsync();
        await RegisterAndLogin();

        // Act
        var response = await Client.GetAsync(Url(ledgerId));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetLedgerStatistics_WhenFromIsAfterTo_422()
    {
        // Arrange
        await RegisterAndLogin();
        var ledgerId = await CreateLedgerAsync();

        // Act
        var response = await Client.GetAsync(Url(ledgerId, "?from=2025-03-01T00:00:00Z&to=2025-01-01T00:00:00Z"));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetLedgerStatistics_WhenLedgerHasFunds_200_WithFundFlowsAndNoTags()
    {
        // Arrange
        await RegisterAndLogin();
        var ledgerId = await CreateLedgerAsync();

        // Act
        var response = await Client.GetAsync(Url(ledgerId));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var fund = json.RootElement.GetProperty("budgets")[0].GetProperty("funds")[0];
        fund.GetProperty("flows").GetProperty("EUR").GetProperty("allocated").GetDecimal().ShouldBe(0);
        fund.GetProperty("flows").GetProperty("EUR").GetProperty("ending").GetDecimal().ShouldBe(123);
        json.RootElement.GetProperty("fundTotals").GetProperty("EUR").GetProperty("ending").GetDecimal().ShouldBe(123);
        json.RootElement.TryGetProperty("tags", out _).ShouldBeFalse();
    }

    private async Task<Guid> CreateLedgerAsync()
    {
        var response = await Client.PostAsJsonAsync("/api/ledgers", LedgersControllerTests.ValidCommand);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<Guid>();
    }
}
