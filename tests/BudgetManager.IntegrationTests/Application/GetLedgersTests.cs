using BudgetManager.Application.Queries;
using BudgetManager.Domain.Entities;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Application;

public class GetLedgersTests(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task GetLedgers_WhenUserHasLedgers_ShouldReturnOnlyOwnLedgersOrderedByName()
    {
        // Arrange
        var otherUserId = await MockAuthenticatedUserAsync();
        await EntityStore.CreateAsync(new Ledger { OwnerId = otherUserId, Name = "Other User Ledger" });
        await EntityStore.SaveChangesAsync();

        var userId = await MockAuthenticatedUserAsync();
        await EntityStore.CreateAsync(new Ledger { OwnerId = userId, Name = "Travel", Description = "trips" });
        await EntityStore.CreateAsync(new Ledger { OwnerId = userId, Name = "Household" });
        await EntityStore.SaveChangesAsync();

        // Act
        var result = await Mediator.Send(new GetLedgersQuery());

        // Assert
        result.Select(x => x.Name).ShouldBe(["Household", "Travel"]);
    }

    [Fact]
    public async Task GetLedgers_WhenUserHasNoLedgers_ShouldReturnEmpty()
    {
        // Arrange
        await MockAuthenticatedUserAsync();

        // Act
        var result = await Mediator.Send(new GetLedgersQuery());

        // Assert
        result.ShouldBeEmpty();
    }
}
