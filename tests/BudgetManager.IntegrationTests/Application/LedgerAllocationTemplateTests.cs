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

/// <summary>
/// Create, update, delete and get of a ledger's allocation templates.
/// </summary>
public class LedgerAllocationTemplateTests(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task CreateLedgerAllocationTemplate_WhenValid_ShouldCreateNamedTemplate()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var (ledger, home, travel) = await SeedTwoBudgetsAsync();

        // Act
        var id = await Mediator.Send(new CreateLedgerAllocationTemplateCommand(ledger.Id, "Salary", "EUR", [
            new(home, 0, AllocationType.Fixed, Amount: 500),
            new(travel, 1, AllocationType.Percent, Percent: 100)]));

        // Assert
        var template = (await Mediator.Send(new GetLedgerAllocationTemplatesQuery(ledger.Id))).ShouldHaveSingleItem();
        template.Id.ShouldBe(id);
        template.Name.ShouldBe("Salary");
        template.Currency.ShouldBe("EUR");
        template.MinimumAmount.ShouldBe(500);
        template.Lines.Select(x => (x.BudgetId, x.BudgetName, x.Sequence, x.Type, x.Amount, x.Percent)).ShouldBe([
            (home, "Budget", 0, AllocationType.Fixed, (decimal?)500, (decimal?)null),
            (travel, "Travel", 1, AllocationType.Percent, null, 100)]);
    }

    [Fact]
    public async Task CreateLedgerAllocationTemplate_WhenNameIsTaken_ShouldThrowConflictException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var (ledger, home, _) = await SeedTwoBudgetsAsync();
        await CreateLedgerTemplateAsync(ledger.Id, "Salary", new LedgerAllocationLine(home, 0, AllocationType.Percent, Percent: 10));

        // Act & Assert
        await Should.ThrowAsync<ConflictException>(() =>
            CreateLedgerTemplateAsync(ledger.Id, "Salary", new LedgerAllocationLine(home, 0, AllocationType.Percent, Percent: 20)));
    }

    [Fact]
    public async Task CreateLedgerAllocationTemplate_WhenBudgetBelongsToAnotherLedger_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var (ledger, _, _) = await SeedTwoBudgetsAsync();
        var otherLedger = await SeedLedgerAsync();

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => CreateLedgerTemplateAsync(
            ledger.Id, "Salary", new LedgerAllocationLine(otherLedger.Budgets.Single().Id, 0, AllocationType.Percent, Percent: 10)));
        ex.Message.ShouldBe("Allocation lines must allocate to budgets of the ledger.");
    }

    [Fact]
    public async Task CreateLedgerAllocationTemplate_WhenLinesAreInvalid_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var (ledger, _, _) = await SeedTwoBudgetsAsync();

        // Act & Assert
        await Should.ThrowAsync<ValidationException>(() => CreateLedgerTemplateAsync(ledger.Id, "Salary"));
    }

    [Fact]
    public async Task CreateLedgerAllocationTemplate_WhenLedgerBelongsToAnotherUser_ShouldThrowAuthorizationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var (ledger, home, _) = await SeedTwoBudgetsAsync();

        await MockAuthenticatedUserAsync();

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() =>
            CreateLedgerTemplateAsync(ledger.Id, "Salary", new LedgerAllocationLine(home, 0, AllocationType.Percent, Percent: 10)));
    }

    [Fact]
    public async Task UpdateLedgerAllocationTemplate_WhenValid_ShouldRenameItAndReplaceItsLines()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var (ledger, home, travel) = await SeedTwoBudgetsAsync();
        var id = await CreateLedgerTemplateAsync(ledger.Id, "Salary", new LedgerAllocationLine(home, 0, AllocationType.Percent, Percent: 10));

        // Act
        await Mediator.Send(new UpdateLedgerAllocationTemplateCommand(ledger.Id, id, "Bonus", [
            new(travel, 0, AllocationType.Fixed, Amount: 50)]));

        // Assert
        var template = (await Mediator.Send(new GetLedgerAllocationTemplatesQuery(ledger.Id))).ShouldHaveSingleItem();
        template.Name.ShouldBe("Bonus");
        template.Lines.ShouldHaveSingleItem().BudgetId.ShouldBe(travel);
    }

    [Fact]
    public async Task UpdateLedgerAllocationTemplate_WhenTemplateBelongsToAnotherLedger_ShouldThrowNotFoundException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var (ledger, home, _) = await SeedTwoBudgetsAsync();
        var otherLedger = await SeedLedgerAsync();
        var id = await CreateLedgerTemplateAsync(ledger.Id, "Salary", new LedgerAllocationLine(home, 0, AllocationType.Percent, Percent: 10));

        // Act & Assert
        await Should.ThrowAsync<NotFoundException>(() => Mediator.Send(new UpdateLedgerAllocationTemplateCommand(
            otherLedger.Id, id, "Salary", [new(otherLedger.Budgets.Single().Id, 0, AllocationType.Percent, Percent: 10)])));
    }

    [Fact]
    public async Task DeleteLedgerAllocationTemplate_WhenItExists_ShouldDeleteIt()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var (ledger, home, _) = await SeedTwoBudgetsAsync();
        var id = await CreateLedgerTemplateAsync(ledger.Id, "Salary", new LedgerAllocationLine(home, 0, AllocationType.Percent, Percent: 10));

        // Act
        await Mediator.Send(new DeleteLedgerAllocationTemplateCommand(ledger.Id, id));

        // Assert
        (await Mediator.Send(new GetLedgerAllocationTemplatesQuery(ledger.Id))).ShouldBeEmpty();
    }

    [Fact]
    public async Task DeleteLedgerAllocationTemplate_WhenTemplateBelongsToAnotherLedger_ShouldThrowNotFoundException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var (ledger, home, _) = await SeedTwoBudgetsAsync();
        var otherLedger = await SeedLedgerAsync();
        var id = await CreateLedgerTemplateAsync(ledger.Id, "Salary", new LedgerAllocationLine(home, 0, AllocationType.Percent, Percent: 10));

        // Act & Assert
        await Should.ThrowAsync<NotFoundException>(() => Mediator.Send(new DeleteLedgerAllocationTemplateCommand(otherLedger.Id, id)));
    }

    [Fact]
    public async Task GetLedgerAllocationTemplates_WhenLedgerBelongsToAnotherUser_ShouldThrowAuthorizationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var (ledger, _, _) = await SeedTwoBudgetsAsync();

        await MockAuthenticatedUserAsync();

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() => Mediator.Send(new GetLedgerAllocationTemplatesQuery(ledger.Id)));
    }

    [Fact]
    public async Task DeleteBudget_WhenLedgerTemplateAllocatesToIt_ShouldDeleteTheTemplatesLinesOfIt()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var (ledger, home, travel) = await SeedTwoBudgetsAsync();
        var id = await CreateLedgerTemplateAsync(ledger.Id, "Salary",
            new LedgerAllocationLine(home, 0, AllocationType.Fixed, Amount: 10),
            new LedgerAllocationLine(travel, 1, AllocationType.Percent, Percent: 10));

        // Act
        await EntityStore.DeleteAsync<Budget>(travel);
        await EntityStore.SaveChangesAsync();

        // Assert
        (await EntityStore.GetAsync<LedgerAllocationTemplateLine>(x => x.LedgerAllocationTemplateId == id, default))
            .ShouldHaveSingleItem().BudgetId.ShouldBe(home);
    }

    /// <summary>
    /// A ledger of the current user with "Budget" (funds Food and Rent) and "Travel" (funds Flights and Hotels).
    /// </summary>
    private async Task<(LedgerDTO Ledger, Guid Home, Guid Travel)> SeedTwoBudgetsAsync()
    {
        var seeded = await SeedLedgerAsync("Food", "Rent");
        var travel = await Mediator.Send(new CreateBudgetCommand(seeded.Id, "Travel", [new("Flights"), new("Hotels")]));
        var ledger = await Mediator.Send(new GetLedgerQuery(seeded.Id));
        return (ledger, ledger.Budgets.Single(x => x.Name == "Budget").Id, travel);
    }

    private Task<Guid> CreateLedgerTemplateAsync(Guid ledgerId, string name, params LedgerAllocationLine[] lines)
        => Mediator.Send(new CreateLedgerAllocationTemplateCommand(ledgerId, name, "EUR", lines));
}
