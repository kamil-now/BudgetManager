using BudgetManager.Application.Commands;
using BudgetManager.Application.Models;
using BudgetManager.Application.Queries;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Models;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Application;

public class GetAllocationTemplatesTests(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task GetAllocationTemplates_WhenBudgetHasNoTemplates_ShouldReturnEmpty()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync();

        // Act
        var result = await Mediator.Send(new GetAllocationTemplatesQuery(ledger.Budgets.Single().Id));

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAllocationTemplates_ShouldReturnLinesOrderedBySequenceWithFundNames()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");
        var budgetId = ledger.Budgets.Single().Id;
        await CreateTemplateAsync(budgetId, "Salary", "EUR",
            new AllocationLine(FundId(ledger, "Food"), 5, AllocationType.Percent, Percent: 20),
            new AllocationLine(FundId(ledger, "Rent"), 2, AllocationType.Fixed, Amount: 300));

        // Act
        var result = await Mediator.Send(new GetAllocationTemplatesQuery(budgetId));

        // Assert
        result.ShouldHaveSingleItem().Lines.Select(x => (x.FundId, x.FundName, x.Sequence)).ShouldBe([
            (FundId(ledger, "Rent"), "Rent", 2),
            (FundId(ledger, "Food"), "Food", 5)]);
    }

    [Fact]
    public async Task GetAllocationTemplates_ShouldReturnNameAndMinimumAmount()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");
        var budgetId = ledger.Budgets.Single().Id;
        await CreateTemplateAsync(budgetId, "Salary", "EUR",
            new AllocationLine(FundId(ledger, "Food"), 0, AllocationType.Percent, Percent: 50),
            new AllocationLine(FundId(ledger, "Rent"), 1, AllocationType.Fixed, Amount: 100));

        // Act
        var result = await Mediator.Send(new GetAllocationTemplatesQuery(budgetId));

        // Assert
        var template = result.ShouldHaveSingleItem();
        template.Name.ShouldBe("Salary");
        template.MinimumAmount.ShouldBe(199.99m);
    }

    [Fact]
    public async Task GetAllocationTemplates_WhenSeveralTemplatesShareCurrency_ShouldReturnAllOrderedByName()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var budgetId = ledger.Budgets.Single().Id;
        var line = new AllocationLine(FundId(ledger, "Food"), 0, AllocationType.Percent, Percent: 10);
        await CreateTemplateAsync(budgetId, "Zeta", "EUR", line);
        await CreateTemplateAsync(budgetId, "Alpha", "EUR", line);

        // Act
        var result = await Mediator.Send(new GetAllocationTemplatesQuery(budgetId));

        // Assert
        result.Select(x => x.Name).ShouldBe(["Alpha", "Zeta"]);
    }

    [Fact]
    public async Task GetAllocationTemplates_WhenBudgetWasCreatedWithTemplates_ShouldReturnThem()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledgerId = await Mediator.Send(new CreateLedgerCommand(
            "Ledger",
            [new("Budget", [new("Food")], [new("Salary", "PLN", [new("Food", 0, AllocationType.Fixed, Amount: 100)])])],
            [new(new(0, "PLN"), "Cash")]));
        var ledger = await Mediator.Send(new GetLedgerQuery(ledgerId));

        // Act
        var result = await Mediator.Send(new GetAllocationTemplatesQuery(ledger.Budgets.Single().Id));

        // Assert
        var template = result.ShouldHaveSingleItem();
        template.Name.ShouldBe("Salary");
        template.Currency.ShouldBe("PLN");
        template.Lines.ShouldHaveSingleItem().Amount.ShouldBe(100);
    }

    [Fact]
    public async Task GetAllocationTemplates_WhenBudgetBelongsToAnotherUser_ShouldThrowAuthorizationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync();

        await MockAuthenticatedUserAsync();

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() => Mediator.Send(new GetAllocationTemplatesQuery(ledger.Budgets.Single().Id)));
    }
}
