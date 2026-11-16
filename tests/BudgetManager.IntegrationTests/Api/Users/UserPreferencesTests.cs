using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Api.Users;

public class UserPreferencesTests(ITestOutputHelper testOutputHelper, ApiFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    private const string Url = "/api/users/me/preferences";

    private static object Body(string theme = "dark") => new
    {
        theme,
        dateFormat = "dd/MM/yyyy",
        defaultCurrency = "USD",
        favoriteCurrencies = new[] { "USD", "EUR" },
        language = "pl"
    };

    [Fact]
    public async Task GetUserPreferences_WhenUserIsUnauthorized_401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.GetAsync(Url);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateUserPreferences_WhenUserIsUnauthorized_401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.PutAsJsonAsync(Url, Body());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUserPreferences_WhenUserIsNew_200_WithDefaults()
    {
        // Arrange
        await RegisterAndLogin();

        // Act
        var response = await Client.GetAsync(Url);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("theme").GetString().ShouldBe("system");
        json.RootElement.GetProperty("dateFormat").GetString().ShouldBe("yyyy-MM-dd");
        json.RootElement.GetProperty("defaultCurrency").GetString().ShouldBe("EUR");
        json.RootElement.GetProperty("favoriteCurrencies").EnumerateArray().Select(x => x.GetString()).ShouldBe(["EUR", "USD", "PLN"]);
        json.RootElement.GetProperty("language").GetString().ShouldBe("en");
    }

    [Fact]
    public async Task UpdateUserPreferences_WhenBodyIsValid_204_AndGetReturnsThem()
    {
        // Arrange
        await RegisterAndLogin();

        // Act
        var response = await Client.PutAsJsonAsync(Url, Body());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await Client.GetStringAsync(Url));
        json.RootElement.GetProperty("theme").GetString().ShouldBe("dark");
        json.RootElement.GetProperty("dateFormat").GetString().ShouldBe("dd/MM/yyyy");
        json.RootElement.GetProperty("defaultCurrency").GetString().ShouldBe("USD");
        json.RootElement.GetProperty("favoriteCurrencies").EnumerateArray().Select(x => x.GetString()).ShouldBe(["USD", "EUR"]);
        json.RootElement.GetProperty("language").GetString().ShouldBe("pl");
    }

    [Fact]
    public async Task UpdateUserPreferences_WhenThemeIsNotAllowed_422()
    {
        // Arrange
        await RegisterAndLogin();

        // Act
        var response = await Client.PutAsJsonAsync(Url, Body(theme: "neon"));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }
}
