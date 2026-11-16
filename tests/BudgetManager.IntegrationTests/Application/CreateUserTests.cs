using BudgetManager.Application.Commands;
using BudgetManager.Application.Models;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Entities;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Application;

public class CreateUserTests(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    private static string NewEmail() => $"test{Guid.NewGuid()}@email.com";

    [Fact]
    public async Task CreateUser_WhenPreferencesAreGiven_ShouldStoreThem()
    {
        // Arrange
        var command = new CreateUserCommand(NewEmail(), "password", "Test User", UserPreferencesTestData.Valid);

        // Act
        var user = await Mediator.Send(command);

        // Assert
        var stored = await EntityStore.GetAsync<User>(user.Id);
        stored.Preferences.ShouldSatisfyAllConditions(
            x => x.Theme.ShouldBe("dark"),
            x => x.DateFormat.ShouldBe("dd.MM.yyyy"),
            x => x.DefaultCurrency.ShouldBe("PLN"),
            x => x.FavoriteCurrencies.ShouldBe(["PLN", "EUR"]),
            x => x.Language.ShouldBe("pl"));
    }

    [Fact]
    public async Task CreateUser_WhenPreferencesAreNull_ShouldStoreDefaults()
    {
        // Arrange
        var command = new CreateUserCommand(NewEmail(), "password", "Test User");

        // Act
        var user = await Mediator.Send(command);

        // Assert
        var stored = await EntityStore.GetAsync<User>(user.Id);
        stored.Preferences.ShouldSatisfyAllConditions(
            x => x.Theme.ShouldBe("system"),
            x => x.DateFormat.ShouldBe("yyyy-MM-dd"),
            x => x.DefaultCurrency.ShouldBe("EUR"),
            x => x.FavoriteCurrencies.ShouldBe(["EUR", "USD", "PLN"]),
            x => x.Language.ShouldBe("en"));
    }

    [Fact]
    public async Task CreateUser_WhenFavoriteCurrenciesAreEmpty_ShouldStoreNone()
    {
        // Arrange
        var command = new CreateUserCommand(NewEmail(), "password", "Test User", UserPreferencesTestData.Valid with { FavoriteCurrencies = [] });

        // Act
        var user = await Mediator.Send(command);

        // Assert
        var stored = await EntityStore.GetAsync<User>(user.Id);
        stored.Preferences.FavoriteCurrencies.ShouldBeEmpty();
    }

    [Theory]
    [ClassData(typeof(UserPreferencesTestData.Invalid))]
    public async Task CreateUser_WhenPreferencesAreInvalid_ShouldThrowValidationException(UserPreferencesDTO preferences, string expectedError)
    {
        // Arrange
        var command = new CreateUserCommand(NewEmail(), "password", "Test User", preferences);

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe(expectedError);
    }

    [Fact]
    public async Task CreateUser_WhenPreferencesAreInvalid_ShouldNotCreateUser()
    {
        // Arrange
        var email = NewEmail();
        var command = new CreateUserCommand(email, "password", "Test User", UserPreferencesTestData.Valid with { Theme = "blue" });

        // Act
        await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));

        // Assert
        (await EntityStore.ExistsAsync<User>(x => x.Email == email)).ShouldBeFalse();
    }
}
