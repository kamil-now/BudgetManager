using BudgetManager.Application.Commands;
using BudgetManager.Application.Models;
using BudgetManager.Application.Queries;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Models;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Application;

public class GetBudgetTests(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    private static CreateLedgerCommand ValidLedger() => new(
        "Ledger",
        [new("Main budget", [new CreateFundDTO("Food")])],
        [new(new(100, "EUR"), "Cash")],
        [new("Main budget", "Food", new(100, "EUR"))]);

    [Fact]
    public async Task GetBudget_WhenBudgetBelongsToUser_ShouldReturnBudgetWithFunds()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var budgetId = await CreateBudgetAsync();

        // Act
        var result = await Mediator.Send(new GetBudgetQuery(budgetId));

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(budgetId);
        result.Name.ShouldBe("Main budget");
        result.Funds.ShouldHaveSingleItem().Name.ShouldBe("Food");
    }

    [Fact]
    public async Task GetBudget_WhenBudgetBelongsToAnotherUser_ShouldThrowAuthorizationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var budgetId = await CreateBudgetAsync();

        await MockAuthenticatedUserAsync();

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() => Mediator.Send(new GetBudgetQuery(budgetId)));
    }

    [Fact]
    public async Task GetBudget_WhenFundsHaveTransactions_ShouldReturnFundAndBudgetBalances()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledgerId = await Mediator.Send(new CreateLedgerCommand(
            "Ledger",
            [new("Main budget",
            [
                new CreateFundDTO("Food"),
                new CreateFundDTO("Rent"),
            ])],
            [new(new(0, "EUR"), "Cash")]));
        var budgetId = (await Mediator.Send(new GetLedgerQuery(ledgerId))).Budgets.Single().Id;
        var funds = (await EntityStore.GetAsync<Fund>(x => x.BudgetId == budgetId, default)).ToArray();
        var food = funds.Single(x => x.Name == "Food");
        var rent = funds.Single(x => x.Name == "Rent");
        foreach (var value in new[] { new Money(100, "EUR"), new Money(50, "EUR"), new Money(30, "USD") })
        {
            await EntityStore.CreateAsync(Allocation.Create(ledgerId, value.Currency, DateTimeOffset.UtcNow, [new FundAmount(food.Id, value.Amount)]));
        }
        await EntityStore.SaveChangesAsync();

        // Act
        var result = await Mediator.Send(new GetBudgetQuery(budgetId));

        // Assert
        var foodBalance = result.Funds.Single(x => x.Id == food.Id).Balance;
        foodBalance.Count.ShouldBe(2);
        foodBalance["EUR"].ShouldBe(150);
        foodBalance["USD"].ShouldBe(30);
        result.Funds.Single(x => x.Id == rent.Id).Balance.ShouldBeEmpty();
        result.Balance.Count.ShouldBe(2);
        result.Balance["EUR"].ShouldBe(150);
        result.Balance["USD"].ShouldBe(30);
    }

    private async Task<Guid> CreateBudgetAsync()
    {
        var ledgerId = await Mediator.Send(ValidLedger());
        var ledger = await Mediator.Send(new GetLedgerQuery(ledgerId));
        return ledger.Budgets.First().Id;
    }
}
