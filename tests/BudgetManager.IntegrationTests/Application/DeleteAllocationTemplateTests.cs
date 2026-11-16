using BudgetManager.Application.Commands;
using BudgetManager.Application.Queries;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Models;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Application;

public class DeleteAllocationTemplateTests(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task DeleteAllocationTemplate_WhenTemplateExists_ShouldDeleteItAndItsLines()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var budgetId = ledger.Budgets.Single().Id;
        var foodId = FundId(ledger, "Food");
        var line = new AllocationLine(foodId, 0, AllocationType.Fixed, Amount: 100);
        var first = await CreateTemplateAsync(budgetId, "First", "EUR", line);
        await CreateTemplateAsync(budgetId, "Second", "EUR", line);

        // Act
        await Mediator.Send(new DeleteAllocationTemplateCommand(budgetId, first));

        // Assert
        (await Mediator.Send(new GetAllocationTemplatesQuery(budgetId))).ShouldHaveSingleItem().Name.ShouldBe("Second");
        (await EntityStore.GetAsync<AllocationTemplateLine>(x => x.FundId == foodId, default)).ShouldHaveSingleItem();
    }

    [Fact]
    public async Task DeleteAllocationTemplate_WhenTemplateDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync();

        // Act & Assert
        await Should.ThrowAsync<NotFoundException>(() => Mediator.Send(new DeleteAllocationTemplateCommand(ledger.Budgets.Single().Id, Guid.NewGuid())));
    }

    [Fact]
    public async Task DeleteAllocationTemplate_WhenTemplateBelongsToAnotherBudget_ShouldThrowNotFoundException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var other = await Mediator.Send(new CreateBudgetCommand(ledger.Id, "Other", [new("Travel")]));
        var templateId = await CreateTemplateAsync(ledger.Budgets.Single().Id, "Template", "EUR", new AllocationLine(FundId(ledger, "Food"), 0, AllocationType.Fixed, Amount: 100));

        // Act & Assert
        await Should.ThrowAsync<NotFoundException>(() => Mediator.Send(new DeleteAllocationTemplateCommand(other, templateId)));
    }

    [Fact]
    public async Task DeleteAllocationTemplate_WhenBudgetBelongsToAnotherUser_ShouldThrowAuthorizationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var budgetId = ledger.Budgets.Single().Id;
        var templateId = await CreateTemplateAsync(budgetId, "Template", "EUR", new AllocationLine(FundId(ledger, "Food"), 0, AllocationType.Fixed, Amount: 100));

        await MockAuthenticatedUserAsync();

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() => Mediator.Send(new DeleteAllocationTemplateCommand(budgetId, templateId)));
    }
}
