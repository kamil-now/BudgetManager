using BudgetManager.Application.Commands;
using BudgetManager.Application.Queries;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Models;
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
        await EntityStore.CreateAsync(new Ledger { OwnerId = userId, Name = "Travel" });
        await EntityStore.CreateAsync(new Ledger { OwnerId = userId, Name = "Household" });
        await EntityStore.SaveChangesAsync();

        // Act
        var result = await Mediator.Send(new GetLedgersQuery());

        // Assert
        result.Select(x => x.Name).ShouldBe(["Household", "Travel"]);
    }

    [Fact]
    public async Task GetLedgers_WhenLedgersHaveTransactions_ShouldReturnEachLedgersAccountTotalPerCurrency()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var household = await SeedLedgerAsync();
        var travel = await SeedLedgerAsync();
        var cash = household.Accounts.Single().Id;
        var card = await Mediator.Send(new CreateAccountCommand(household.Id, new(0, "EUR"), "Card"));
        await Mediator.Send(new CreateAccountTransactionCommand(cash, new(100, "EUR"), DateTimeOffset.UtcNow, "Salary"));
        await Mediator.Send(new CreateAccountTransactionCommand(card, new(-30, "EUR"), DateTimeOffset.UtcNow, "Groceries"));
        await Mediator.Send(new CreateAccountTransactionCommand(cash, new(20, "USD"), DateTimeOffset.UtcNow, "Gift"));
        await Mediator.Send(new CreateAccountTransactionCommand(travel.Accounts.Single().Id, new(50, "PLN"), DateTimeOffset.UtcNow, "Refund"));
        await Mediator.Send(new CreateAccountTransactionCommand(travel.Accounts.Single().Id, new(-50, "PLN"), DateTimeOffset.UtcNow, "Tickets"));

        // Act
        var result = await Mediator.Send(new GetLedgersQuery());

        // Assert
        result.Single(x => x.Id == household.Id).Balance.ShouldBe(new Balance { ["EUR"] = 70, ["USD"] = 20 }, ignoreOrder: true);
        result.Single(x => x.Id == travel.Id).Balance.ShouldBeEmpty();
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
