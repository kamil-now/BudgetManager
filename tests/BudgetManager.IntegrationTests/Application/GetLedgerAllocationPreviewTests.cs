using BudgetManager.Application.Commands;
using BudgetManager.Application.Models;
using BudgetManager.Application.Queries;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Models;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Application;

public class GetLedgerAllocationPreviewTests(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task GetLedgerAllocationPreview_WhenAllocationUsesTemplate_ShouldReturnTemplateSplit()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");
        var templateId = await CreateTemplateAsync(ledger.Budgets.Single().Id, "Salary", "EUR",
            new AllocationLine(FundId(ledger, "Food"), 0, AllocationType.Fixed, Amount: 100),
            new AllocationLine(FundId(ledger, "Rent"), 1, AllocationType.Percent, Percent: 50));

        // Act
        var result = await Mediator.Send(new GetLedgerAllocationPreviewQuery(ledger.Id, 123.45m, "EUR", new AllocationInput(TemplateId: templateId)));

        // Assert
        result.Lines.ShouldBe([
            new AllocationPreviewDTO.Line(FundId(ledger, "Food"), "Food", "Budget", 100),
            new AllocationPreviewDTO.Line(FundId(ledger, "Rent"), "Rent", "Budget", 11.72m)]);
        result.Unallocated.ShouldBe(11.73m);
    }

    [Fact]
    public async Task GetLedgerAllocationPreview_WhenAllocationHasCustomLines_ShouldSplitThemLikeATemplate()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var seeded = await SeedLedgerAsync("Food");
        await Mediator.Send(new CreateBudgetCommand(seeded.Id, "Other", [new CreateFundDTO("Travel")]));
        var ledger = await Mediator.Send(new GetLedgerQuery(seeded.Id));
        var food = FundId(ledger, "Food");
        var allocation = new AllocationInput(Lines: [
            new(food, 0, AllocationType.Fixed, 30),
            new(FundId(ledger, "Travel"), 1, AllocationType.Percent, Percent: 50),
            new(food, 2, AllocationType.Percent, Percent: 25)]);

        // Act
        var result = await Mediator.Send(new GetLedgerAllocationPreviewQuery(ledger.Id, 130, "EUR", allocation));

        // Assert
        result.Lines.ShouldBe([
            new AllocationPreviewDTO.Line(food, "Food", "Budget", 30),
            new AllocationPreviewDTO.Line(FundId(ledger, "Travel"), "Travel", "Other", 50),
            new AllocationPreviewDTO.Line(food, "Food", "Budget", 12.5m)]);
        result.Unallocated.ShouldBe(37.5m);
    }

    [Fact]
    public async Task GetLedgerAllocationPreview_WhenAmountIsBelowTemplateMinimum_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var templateId = await CreateTemplateAsync(ledger.Budgets.Single().Id, "Salary", "EUR", new AllocationLine(FundId(ledger, "Food"), 0, AllocationType.Fixed, Amount: 100));

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(new GetLedgerAllocationPreviewQuery(ledger.Id, 99.99m, "EUR", new AllocationInput(TemplateId: templateId))));
        ex.Message.ShouldBe("Amount 99.99 EUR is below the allocation's minimum of 100 EUR.");
    }

    [Fact]
    public async Task GetLedgerAllocationPreview_WhenTemplateIsInAnotherCurrency_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var templateId = await CreateTemplateAsync(ledger.Budgets.Single().Id, "Salary", "USD", new AllocationLine(FundId(ledger, "Food"), 0, AllocationType.Percent, Percent: 10));

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(new GetLedgerAllocationPreviewQuery(ledger.Id, 100, "EUR", new AllocationInput(TemplateId: templateId))));
        ex.Message.ShouldBe("Allocation template is in USD, the amount is in EUR.");
    }

    [Fact]
    public async Task GetLedgerAllocationPreview_WhenTemplateBelongsToAnotherLedger_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var otherLedger = await SeedLedgerAsync("Rent");
        var templateId = await CreateTemplateAsync(otherLedger.Budgets.Single().Id, "Other", "EUR", new AllocationLine(FundId(otherLedger, "Rent"), 0, AllocationType.Percent, Percent: 10));

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(new GetLedgerAllocationPreviewQuery(ledger.Id, 100, "EUR", new AllocationInput(TemplateId: templateId))));
        ex.Message.ShouldBe("Allocation template must belong to the account's ledger.");
    }

    [Fact]
    public async Task GetLedgerAllocationPreview_WhenTemplateDoesNotExist_ShouldThrowAuthorizationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() => Mediator.Send(new GetLedgerAllocationPreviewQuery(ledger.Id, 100, "EUR", new AllocationInput(TemplateId: Guid.NewGuid()))));
    }

    [Fact]
    public async Task GetLedgerAllocationPreview_WhenFundBelongsToAnotherLedger_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var otherLedger = await SeedLedgerAsync("Rent");
        var allocation = new AllocationInput(Lines: [new(FundId(otherLedger, "Rent"), 0, AllocationType.Fixed, 10)]);

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(new GetLedgerAllocationPreviewQuery(ledger.Id, 100, "EUR", allocation)));
        ex.Message.ShouldBe("Allocated funds must belong to a budget in the account's ledger.");
    }

    [Theory]
    [InlineData(0, "EUR", "Amount must be positive.")]
    [InlineData(10, "EU", "Currency value 'EU' is not a valid currency code.")]
    public async Task GetLedgerAllocationPreview_WhenQueryIsInvalid_ShouldThrowValidationException(decimal amount, string currency, string expectedError)
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var allocation = new AllocationInput(Lines: [new(FundId(ledger, "Food"), 0, AllocationType.Fixed, 10)]);

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(new GetLedgerAllocationPreviewQuery(ledger.Id, amount, currency, allocation)));
        ex.Message.ShouldBe(expectedError);
    }

    [Fact]
    public async Task GetLedgerAllocationPreview_WhenLedgerBelongsToAnotherUser_ShouldThrowAuthorizationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var allocation = new AllocationInput(Lines: [new(FundId(ledger, "Food"), 0, AllocationType.Fixed, 10)]);

        await MockAuthenticatedUserAsync();

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() => Mediator.Send(new GetLedgerAllocationPreviewQuery(ledger.Id, 100, "EUR", allocation)));
    }
}
