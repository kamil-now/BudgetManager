using BudgetManager.Application.Commands;
using BudgetManager.Application.Queries;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Models;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Application;

public class CreateAllocationTemplateTests(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task CreateAllocationTemplate_WhenValid_ShouldCreateNamedTemplate()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");
        var budgetId = ledger.Budgets.Single().Id;

        // Act
        var id = await CreateTemplateAsync(budgetId, "Salary", "EUR",
            new AllocationLine(FundId(ledger, "Rent"), 0, AllocationType.Fixed, Amount: 800),
            new AllocationLine(FundId(ledger, "Food"), 1, AllocationType.Percent, Percent: 100));

        // Assert
        var template = (await Mediator.Send(new GetAllocationTemplatesQuery(budgetId))).ShouldHaveSingleItem();
        template.Id.ShouldBe(id);
        template.Name.ShouldBe("Salary");
        template.Currency.ShouldBe("EUR");
        template.MinimumAmount.ShouldBe(800);
        template.Lines.Count().ShouldBe(2);
    }

    [Fact]
    public async Task CreateAllocationTemplate_WhenAnotherTemplateHasSameCurrency_ShouldCreateIt()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var budgetId = ledger.Budgets.Single().Id;
        var line = new AllocationLine(FundId(ledger, "Food"), 0, AllocationType.Percent, Percent: 10);
        await CreateTemplateAsync(budgetId, "First", "EUR", line);

        // Act
        await CreateTemplateAsync(budgetId, "Second", "EUR", line);

        // Assert
        (await Mediator.Send(new GetAllocationTemplatesQuery(budgetId))).Count().ShouldBe(2);
    }

    [Fact]
    public async Task CreateAllocationTemplate_WhenNameIsTaken_ShouldThrowConflictException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var budgetId = ledger.Budgets.Single().Id;
        var line = new AllocationLine(FundId(ledger, "Food"), 0, AllocationType.Percent, Percent: 10);
        await CreateTemplateAsync(budgetId, "Salary", "EUR", line);

        // Act & Assert
        await Should.ThrowAsync<ConflictException>(() => CreateTemplateAsync(budgetId, "Salary", "USD", line));
    }

    [Theory]
    [InlineData("")]
    public async Task CreateAllocationTemplate_WhenNameIsEmpty_ShouldThrowValidationException(string name)
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");

        // Act & Assert
        await Should.ThrowAsync<ValidationException>(() => CreateTemplateAsync(
            ledger.Budgets.Single().Id, name, "EUR", new AllocationLine(FundId(ledger, "Food"), 0, AllocationType.Percent, Percent: 10)));
    }

    [Fact]
    public async Task CreateAllocationTemplate_WhenNameIsTooLong_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");

        // Act & Assert
        await Should.ThrowAsync<ValidationException>(() => CreateTemplateAsync(
            ledger.Budgets.Single().Id, new string('a', Constants.MaxNameLength + 1), "EUR", new AllocationLine(FundId(ledger, "Food"), 0, AllocationType.Percent, Percent: 10)));
    }

    [Fact]
    public async Task CreateAllocationTemplate_WhenCurrencyIsInvalid_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");

        // Act & Assert
        await Should.ThrowAsync<ValidationException>(() => CreateTemplateAsync(
            ledger.Budgets.Single().Id, "Salary", "eur", new AllocationLine(FundId(ledger, "Food"), 0, AllocationType.Percent, Percent: 10)));
    }

    [Fact]
    public async Task CreateAllocationTemplate_WhenThereAreNoLines_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");

        // Act & Assert
        await Should.ThrowAsync<ValidationException>(() => CreateTemplateAsync(ledger.Budgets.Single().Id, "Salary", "EUR"));
    }

    [Fact]
    public async Task CreateAllocationTemplate_WhenHundredPercentStepIsNotLast_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => CreateTemplateAsync(
            ledger.Budgets.Single().Id,
            "Salary",
            "EUR",
            new AllocationLine(FundId(ledger, "Food"), 0, AllocationType.Percent, Percent: 100),
            new AllocationLine(FundId(ledger, "Rent"), 1, AllocationType.Fixed, Amount: 10)));
        ex.Message.ShouldBe("A 100% step takes everything that is left, so it must be the last step.");
    }

    [Fact]
    public async Task CreateAllocationTemplate_WhenLineAllocatesToFundOfAnotherBudget_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var otherBudgetId = await Mediator.Send(new CreateBudgetCommand(ledger.Id, "Other", [new("Travel")]));
        var otherLedger = await Mediator.Send(new GetLedgerQuery(ledger.Id));
        var travelId = FundId(otherLedger, "Travel");

        // Act & Assert
        await Should.ThrowAsync<ValidationException>(() => CreateTemplateAsync(
            ledger.Budgets.Single(x => x.Id != otherBudgetId).Id, "Salary", "EUR", new AllocationLine(travelId, 0, AllocationType.Percent, Percent: 10)));
    }

    [Fact]
    public async Task CreateAllocationTemplate_WhenBudgetBelongsToAnotherUser_ShouldThrowAuthorizationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");

        await MockAuthenticatedUserAsync();

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() => CreateTemplateAsync(
            ledger.Budgets.Single().Id, "Salary", "EUR", new AllocationLine(FundId(ledger, "Food"), 0, AllocationType.Percent, Percent: 10)));
    }
}
