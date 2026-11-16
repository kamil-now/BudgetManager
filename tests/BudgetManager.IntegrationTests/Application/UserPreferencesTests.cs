using BudgetManager.Application.Commands;
using BudgetManager.Application.Models;
using BudgetManager.Application.Queries;
using BudgetManager.Application.Validators;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Application;

public class UserPreferencesTests(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    private static readonly UpdateUserPreferencesCommand ValidCommand = new(
        UserPreferencesTestData.Valid.Theme,
        UserPreferencesTestData.Valid.DateFormat,
        UserPreferencesTestData.Valid.DefaultCurrency,
        UserPreferencesTestData.Valid.FavoriteCurrencies,
        UserPreferencesTestData.Valid.Language);

    [Fact]
    public async Task GetUserPreferences_WhenNothingWasSet_ShouldReturnDefaults()
    {
        // Arrange
        await MockAuthenticatedUserAsync();

        // Act
        var result = await Mediator.Send(new GetUserPreferencesQuery());

        // Assert
        result.ShouldSatisfyAllConditions(
            x => x.Theme.ShouldBe("system"),
            x => x.DateFormat.ShouldBe("yyyy-MM-dd"),
            x => x.DefaultCurrency.ShouldBe("EUR"),
            x => x.FavoriteCurrencies.ShouldBe(["EUR", "USD", "PLN"]),
            x => x.Language.ShouldBe("en"));
    }

    [Fact]
    public async Task UpdateUserPreferences_WhenCommandIsValid_ShouldStoreThem()
    {
        // Arrange
        await MockAuthenticatedUserAsync();

        // Act
        await Mediator.Send(ValidCommand);

        // Assert
        var result = await Mediator.Send(new GetUserPreferencesQuery());
        result.ShouldSatisfyAllConditions(
            x => x.Theme.ShouldBe("dark"),
            x => x.DateFormat.ShouldBe("dd.MM.yyyy"),
            x => x.DefaultCurrency.ShouldBe("PLN"),
            x => x.FavoriteCurrencies.ShouldBe(["PLN", "EUR"]),
            x => x.Language.ShouldBe("pl"));
    }

    [Fact]
    public async Task UpdateUserPreferences_WhenFavoriteCurrenciesAreEmpty_ShouldStoreNone()
    {
        // Arrange
        await MockAuthenticatedUserAsync();

        // Act
        await Mediator.Send(ValidCommand with { FavoriteCurrencies = [] });

        // Assert
        (await Mediator.Send(new GetUserPreferencesQuery())).FavoriteCurrencies.ShouldBeEmpty();
    }

    [Fact]
    public async Task UpdateUserPreferences_ShouldNotChangeOtherUsersPreferences()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var otherUser = MockCurrentUserService.MockUserId;
        await MockAuthenticatedUserAsync();

        // Act
        await Mediator.Send(ValidCommand);

        // Assert
        MockCurrentUserService.MockUserId = otherUser;
        (await Mediator.Send(new GetUserPreferencesQuery())).Theme.ShouldBe("system");
    }

    public sealed class InvalidCommands : TheoryData<UpdateUserPreferencesCommand, string>
    {
        public InvalidCommands()
        {
            foreach (var row in new UserPreferencesTestData.Invalid())
            {
                var p = (UserPreferencesDTO)row[0];
                Add(new(p.Theme, p.DateFormat, p.DefaultCurrency, p.FavoriteCurrencies, p.Language), (string)row[1]);
            }
        }
    }

    [Theory]
    [ClassData(typeof(InvalidCommands))]
    public async Task UpdateUserPreferences_WhenCommandIsInvalid_ShouldThrowValidationException(UpdateUserPreferencesCommand command, string expectedError)
    {
        // Arrange
        await MockAuthenticatedUserAsync();

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe(expectedError);
    }

    [Fact]
    public async Task GetUserPreferences_WhenUserIsNotAuthenticated_ShouldThrowAuthenticationException()
    {
        // Arrange
        MockUnauthenticatedUser();

        // Act & Assert
        await Should.ThrowAsync<AuthenticationException>(() => Mediator.Send(new GetUserPreferencesQuery()));
    }

    [Fact]
    public async Task UpdateUserPreferences_WhenUserIsNotAuthenticated_ShouldThrowAuthenticationException()
    {
        // Arrange
        MockUnauthenticatedUser();

        // Act & Assert
        await Should.ThrowAsync<AuthenticationException>(() => Mediator.Send(ValidCommand));
    }
}
