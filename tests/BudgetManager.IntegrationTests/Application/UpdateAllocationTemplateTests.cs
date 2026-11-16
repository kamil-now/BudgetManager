using BudgetManager.Application.Commands;
using BudgetManager.Application.Queries;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Models;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Application;

public class UpdateAllocationTemplateTests(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task UpdateAllocationTemplate_WhenValid_ShouldRenameAndReplaceLinesKeepingCurrency()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");
        var budgetId = ledger.Budgets.Single().Id;
        var id = await CreateTemplateAsync(budgetId, "Old", "EUR", new AllocationLine(FundId(ledger, "Food"), 0, AllocationType.Fixed, Amount: 100));

        // Act
        await Mediator.Send(new UpdateAllocationTemplateCommand(budgetId, id, "New", [
            new AllocationLine(FundId(ledger, "Rent"), 0, AllocationType.Percent, Percent: 40),
            new AllocationLine(FundId(ledger, "Food"), 1, AllocationType.Percent, Percent: 100)]));

        // Assert
        var template = (await Mediator.Send(new GetAllocationTemplatesQuery(budgetId))).ShouldHaveSingleItem();
        template.Name.ShouldBe("New");
        template.Currency.ShouldBe("EUR");
        template.Lines.Select(x => x.FundName).ShouldBe(["Rent", "Food"]);
        (await EntityStore.GetAsync<AllocationTemplateLine>(x => x.AllocationTemplateId == id, default)).Count().ShouldBe(2);
    }

    [Fact]
    public async Task UpdateAllocationTemplate_WhenNameIsUnchanged_ShouldNotConflictWithItself()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var budgetId = ledger.Budgets.Single().Id;
        var line = new AllocationLine(FundId(ledger, "Food"), 0, AllocationType.Percent, Percent: 10);
        var id = await CreateTemplateAsync(budgetId, "Salary", "EUR", line);

        // Act & Assert
        await Should.NotThrowAsync(() => Mediator.Send(new UpdateAllocationTemplateCommand(budgetId, id, "Salary", [line])));
    }

    [Fact]
    public async Task UpdateAllocationTemplate_WhenNameIsTakenByAnotherTemplate_ShouldThrowConflictException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var budgetId = ledger.Budgets.Single().Id;
        var line = new AllocationLine(FundId(ledger, "Food"), 0, AllocationType.Percent, Percent: 10);
        await CreateTemplateAsync(budgetId, "Taken", "EUR", line);
        var id = await CreateTemplateAsync(budgetId, "Other", "EUR", line);

        // Act & Assert
        await Should.ThrowAsync<ConflictException>(() => Mediator.Send(new UpdateAllocationTemplateCommand(budgetId, id, "Taken", [line])));
    }

    [Fact]
    public async Task UpdateAllocationTemplate_WhenTemplateDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var line = new AllocationLine(FundId(ledger, "Food"), 0, AllocationType.Percent, Percent: 10);

        // Act & Assert
        await Should.ThrowAsync<NotFoundException>(() => Mediator.Send(new UpdateAllocationTemplateCommand(ledger.Budgets.Single().Id, Guid.NewGuid(), "Name", [line])));
    }

    [Fact]
    public async Task UpdateAllocationTemplate_WhenTemplateBelongsToAnotherBudget_ShouldThrowNotFoundException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var line = new AllocationLine(FundId(ledger, "Food"), 0, AllocationType.Percent, Percent: 10);
        var id = await CreateTemplateAsync(ledger.Budgets.Single().Id, "Salary", "EUR", line);
        var otherBudgetId = await Mediator.Send(new CreateBudgetCommand(ledger.Id, "Other", [new("Travel")]));

        // Act & Assert
        await Should.ThrowAsync<NotFoundException>(() => Mediator.Send(new UpdateAllocationTemplateCommand(otherBudgetId, id, "Salary", [line])));
    }

    [Fact]
    public async Task UpdateAllocationTemplate_WhenStepsAreInvalid_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var budgetId = ledger.Budgets.Single().Id;
        var line = new AllocationLine(FundId(ledger, "Food"), 0, AllocationType.Percent, Percent: 10);
        var id = await CreateTemplateAsync(budgetId, "Salary", "EUR", line);

        // Act & Assert
        await Should.ThrowAsync<ValidationException>(() => Mediator.Send(new UpdateAllocationTemplateCommand(
            budgetId, id, "Salary", [new AllocationLine(FundId(ledger, "Food"), 0, AllocationType.Percent, Percent: 0)])));
    }

    [Fact]
    public async Task UpdateAllocationTemplate_WhenBudgetBelongsToAnotherUser_ShouldThrowAuthorizationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var budgetId = ledger.Budgets.Single().Id;
        var line = new AllocationLine(FundId(ledger, "Food"), 0, AllocationType.Percent, Percent: 10);
        var id = await CreateTemplateAsync(budgetId, "Salary", "EUR", line);

        await MockAuthenticatedUserAsync();

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() => Mediator.Send(new UpdateAllocationTemplateCommand(budgetId, id, "Salary", [line])));
    }
}
