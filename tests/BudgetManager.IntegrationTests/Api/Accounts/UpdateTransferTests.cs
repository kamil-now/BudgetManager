using System.Net;
using System.Net.Http.Json;
using BudgetManager.Application.Commands;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Api.Accounts;

public class UpdateTransferTests(ITestOutputHelper testOutputHelper, ApiFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task UpdateTransfer_WhenUserIsUnauthorized_401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.PutAsJsonAsync($"/api/accounts/{Guid.NewGuid()}/transfer/{Guid.NewGuid()}", new { });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateTransfer_WhenRequestIsValid_OK()
    {
        // Arrange
        await RegisterAndLogin();
        var (source, target) = await CreateAccountsAsync();
        var transferId = await PostAsync($"/api/accounts/{source}/transfer",
            new CreateAccountTransferCommand(source, target, new(100, "EUR"), DateTimeOffset.UtcNow, null));

        // Act
        var response = await Client.PutAsJsonAsync($"/api/accounts/{source}/transfer/{transferId}",
            new UpdateAccountTransferCommand(transferId, source, target, new(50, "EUR"), DateTimeOffset.UtcNow, null));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task UpdateTransfer_WhenValueIsNegative_422()
    {
        // Arrange
        await RegisterAndLogin();
        var (source, target) = await CreateAccountsAsync();
        var transferId = await PostAsync($"/api/accounts/{source}/transfer",
            new CreateAccountTransferCommand(source, target, new(100, "EUR"), DateTimeOffset.UtcNow, null));

        // Act
        var response = await Client.PutAsJsonAsync($"/api/accounts/{source}/transfer/{transferId}",
            new UpdateAccountTransferCommand(transferId, source, target, new(-50, "EUR"), DateTimeOffset.UtcNow, null));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateCurrencyExchange_WhenUserIsUnauthorized_401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.PutAsJsonAsync($"/api/accounts/{Guid.NewGuid()}/currencyExchange/{Guid.NewGuid()}", new { });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateCurrencyExchange_WhenRequestIsValid_OK()
    {
        // Arrange
        await RegisterAndLogin();
        var (source, _) = await CreateAccountsAsync();
        var exchangeId = await PostAsync($"/api/accounts/{source}/currencyExchange",
            new CreateCurrencyExchangeCommand(source, null, new(90, "USD"), new(100, "EUR"), DateTimeOffset.UtcNow, null));

        // Act
        var response = await Client.PutAsJsonAsync($"/api/accounts/{source}/currencyExchange/{exchangeId}",
            new UpdateCurrencyExchangeCommand(exchangeId, source, null, new(45, "USD"), new(50, "EUR"), DateTimeOffset.UtcNow, null));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    private async Task<(Guid Source, Guid Target)> CreateAccountsAsync()
    {
        var ledgerResponse = await Client.PostAsJsonAsync("/api/ledgers", Ledgers.LedgersControllerTests.ValidCommand);
        ledgerResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var ledgerId = await ledgerResponse.Content.ReadFromJsonAsync<Guid>();

        var source = await PostAsync("/api/accounts", new CreateAccountCommand(ledgerId, new(0, "EUR"), $"Source {Guid.NewGuid()}", null));
        var target = await PostAsync("/api/accounts", new CreateAccountCommand(ledgerId, new(0, "EUR"), $"Target {Guid.NewGuid()}", null));
        return (source, target);
    }

    private async Task<Guid> PostAsync<T>(string url, T body)
    {
        var response = await Client.PostAsJsonAsync(url, body);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<Guid>();
    }
}
