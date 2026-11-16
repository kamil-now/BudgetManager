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
/// Allocating an income by a ledger's allocation template: between budgets first, then by a picked template of each budget.
/// </summary>
public class LedgerTemplateAllocationTests(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task CreateAccountTransaction_WhenAllocationUsesLedgerTemplate_ShouldSplitBetweenBudgetsThenFunds()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var seed = await SeedAsync();

        // Act
        var id = await Mediator.Send(new CreateAccountTransactionCommand(
            seed.AccountId, new(1000, "EUR"), DateTimeOffset.UtcNow, "Salary",
            Allocation: new AllocationInput(LedgerTemplate: new(seed.LedgerTemplate, [seed.HomeTemplate, seed.TravelTemplate]))));

        // Assert
        // ledger: Budget gets 70% = 700, Travel 30% = 300
        // Budget: Rent 400, then Food 50% of the remaining 300 = 150; Travel: Flights 100%
        (await AllocationsOfAsync(id)).Select(x => (x.FundId, x.Value.Amount)).ShouldBe([
            (seed.Rent, 400m),
            (seed.Food, 150m),
            (seed.Flights, 300m)], ignoreOrder: true);
    }

    [Fact]
    public async Task CreateAccountTransaction_WhenBudgetHasSeveralLines_ShouldSplitTheirSumByItsTemplate()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var seed = await SeedAsync();
        var ledgerTemplate = await Mediator.Send(new CreateLedgerAllocationTemplateCommand(seed.LedgerId, "Twice", "EUR", [
            new(seed.Travel, 0, AllocationType.Fixed, Amount: 100),
            new(seed.Travel, 1, AllocationType.Percent, Percent: 10)]));

        // Act
        var id = await Mediator.Send(new CreateAccountTransactionCommand(
            seed.AccountId, new(1100, "EUR"), DateTimeOffset.UtcNow, "Salary",
            Allocation: new AllocationInput(LedgerTemplate: new(ledgerTemplate, [seed.TravelTemplate]))));

        // Assert
        (await AllocationsOfAsync(id)).ShouldHaveSingleItem().Value.Amount.ShouldBe(200);
    }

    [Fact]
    public async Task CreateAccountTransaction_WhenBudgetHasNoPickedTemplate_ShouldThrowValidationExceptionAndCreateNothing()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var seed = await SeedAsync();

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(new CreateAccountTransactionCommand(
            seed.AccountId, new(1000, "EUR"), DateTimeOffset.UtcNow, "Salary",
            Allocation: new AllocationInput(LedgerTemplate: new(seed.LedgerTemplate, [seed.HomeTemplate])))));
        ex.Message.ShouldBe("Every budget of the ledger allocation template needs an allocation template.");
        (await UnallocatedAsync(seed.LedgerId)).ShouldBe(0);
    }

    [Fact]
    public async Task CreateAccountTransaction_WhenBudgetHasTwoPickedTemplates_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var seed = await SeedAsync();
        var secondTravelTemplate = await CreateTemplateAsync(seed.Travel, "Hotels only", "EUR",
            new AllocationLine(seed.Hotels, 0, AllocationType.Percent, Percent: 100));

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(new CreateAccountTransactionCommand(
            seed.AccountId, new(1000, "EUR"), DateTimeOffset.UtcNow, "Salary",
            Allocation: new AllocationInput(LedgerTemplate: new(seed.LedgerTemplate, [seed.HomeTemplate, seed.TravelTemplate, secondTravelTemplate])))));
        ex.Message.ShouldBe("Only one allocation template can be picked per budget.");
    }

    [Fact]
    public async Task CreateAccountTransaction_WhenPickedTemplateIsOfAnotherBudget_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var seed = await SeedAsync();
        var ledgerTemplate = await Mediator.Send(new CreateLedgerAllocationTemplateCommand(seed.LedgerId, "Travel only", "EUR", [
            new(seed.Travel, 0, AllocationType.Percent, Percent: 100)]));

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(new CreateAccountTransactionCommand(
            seed.AccountId, new(1000, "EUR"), DateTimeOffset.UtcNow, "Salary",
            Allocation: new AllocationInput(LedgerTemplate: new(ledgerTemplate, [seed.HomeTemplate])))));
        ex.Message.ShouldBe("Budget allocation templates must belong to budgets of the ledger allocation template.");
    }

    [Fact]
    public async Task CreateAccountTransaction_WhenBudgetShareIsBelowItsTemplatesMinimum_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var seed = await SeedAsync();

        // Act & Assert
        // Budget gets 70% of 500 = 350, below the 400 of Rent
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(new CreateAccountTransactionCommand(
            seed.AccountId, new(500, "EUR"), DateTimeOffset.UtcNow, "Salary",
            Allocation: new AllocationInput(LedgerTemplate: new(seed.LedgerTemplate, [seed.HomeTemplate, seed.TravelTemplate])))));
        ex.Message.ShouldBe("Share 350 EUR of allocation template Home is below its minimum of 400 EUR.");
    }

    [Fact]
    public async Task CreateAccountTransaction_WhenLedgerTemplateIsInAnotherCurrency_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var seed = await SeedAsync();

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(new CreateAccountTransactionCommand(
            seed.AccountId, new(1000, "USD"), DateTimeOffset.UtcNow, "Salary",
            Allocation: new AllocationInput(LedgerTemplate: new(seed.LedgerTemplate, [seed.HomeTemplate, seed.TravelTemplate])))));
        ex.Message.ShouldBe("Ledger allocation template is in EUR, the amount is in USD.");
    }

    [Fact]
    public async Task CreateAccountTransaction_WhenPickedTemplateIsInAnotherCurrency_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var seed = await SeedAsync();
        var usdLedgerTemplate = await Mediator.Send(new CreateLedgerAllocationTemplateCommand(seed.LedgerId, "Dollars", "USD", [
            new(seed.Travel, 0, AllocationType.Percent, Percent: 100)]));

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(new CreateAccountTransactionCommand(
            seed.AccountId, new(1000, "USD"), DateTimeOffset.UtcNow, "Salary",
            Allocation: new AllocationInput(LedgerTemplate: new(usdLedgerTemplate, [seed.TravelTemplate])))));
        ex.Message.ShouldBe("Allocation template Trips is in EUR, the amount is in USD.");
    }

    [Fact]
    public async Task CreateAccountTransaction_WhenLedgerTemplateBelongsToAnotherLedger_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var seed = await SeedAsync();
        var otherLedger = await SeedLedgerAsync();

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(new CreateAccountTransactionCommand(
            otherLedger.Accounts.Single().Id, new(1000, "EUR"), DateTimeOffset.UtcNow, "Salary",
            Allocation: new AllocationInput(LedgerTemplate: new(seed.LedgerTemplate, [seed.HomeTemplate, seed.TravelTemplate])))));
        ex.Message.ShouldBe("Ledger allocation template must belong to the account's ledger.");
    }

    [Fact]
    public async Task CreateAccountTransaction_WhenLedgerTemplateBelongsToAnotherUser_ShouldThrowAuthorizationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var seed = await SeedAsync();

        await MockAuthenticatedUserAsync();
        var ownLedger = await SeedLedgerAsync();

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() => Mediator.Send(new CreateAccountTransactionCommand(
            ownLedger.Accounts.Single().Id, new(1000, "EUR"), DateTimeOffset.UtcNow, "Salary",
            Allocation: new AllocationInput(LedgerTemplate: new(seed.LedgerTemplate, [seed.HomeTemplate, seed.TravelTemplate])))));
    }

    [Fact]
    public async Task UpdateAccountTransaction_WhenAllocationUsesLedgerTemplate_ShouldReplaceTheAllocations()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var seed = await SeedAsync();
        var date = DateTimeOffset.UtcNow;
        var id = await Mediator.Send(new CreateAccountTransactionCommand(
            seed.AccountId, new(100, "EUR"), date, "Salary",
            Allocation: new AllocationInput(Lines: [new(seed.Food, 0, AllocationType.Fixed, 100)])));

        // Act
        await Mediator.Send(new UpdateAccountTransactionCommand(id, seed.AccountId, new(1000, "EUR"), date, "Salary",
            Allocation: new AllocationInput(LedgerTemplate: new(seed.LedgerTemplate, [seed.HomeTemplate, seed.TravelTemplate]))));

        // Assert
        (await AllocationsOfAsync(id)).Sum(x => x.Value.Amount).ShouldBe(850);
    }

    [Fact]
    public async Task GetLedgerAllocationPreview_WhenAllocationUsesLedgerTemplate_ShouldReturnFundAmountsAndUnallocated()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var seed = await SeedAsync();

        // Act
        var preview = await Mediator.Send(new GetLedgerAllocationPreviewQuery(seed.LedgerId, 1000, "EUR",
            new AllocationInput(LedgerTemplate: new(seed.LedgerTemplate, [seed.HomeTemplate, seed.TravelTemplate]))));

        // Assert
        preview.Lines.Select(x => (x.FundId, x.Amount)).ShouldBe([
            (seed.Rent, 400m),
            (seed.Food, 150m),
            (seed.Flights, 300m)], ignoreOrder: true);
        preview.Unallocated.ShouldBe(150);
    }

    private sealed record Seed(
        Guid LedgerId,
        Guid AccountId,
        Guid Travel,
        Guid Food,
        Guid Rent,
        Guid Flights,
        Guid Hotels,
        Guid HomeTemplate,
        Guid TravelTemplate,
        Guid LedgerTemplate);

    /// <summary>
    /// A ledger with "Budget" (Food, Rent; template "Home": Rent 400, then Food 50%) and "Travel" (Flights, Hotels;
    /// template "Trips": Flights 100%), and the ledger template "Salary": Budget 70%, then Travel 100%.
    /// </summary>
    private async Task<Seed> SeedAsync()
    {
        var seeded = await SeedLedgerAsync("Food", "Rent");
        var travel = await Mediator.Send(new CreateBudgetCommand(seeded.Id, "Travel", [new("Flights"), new("Hotels")]));
        var ledger = await Mediator.Send(new GetLedgerQuery(seeded.Id));
        var home = ledger.Budgets.Single(x => x.Name == "Budget").Id;

        var homeTemplate = await CreateTemplateAsync(home, "Home", "EUR",
            new AllocationLine(FundId(ledger, "Rent"), 0, AllocationType.Fixed, Amount: 400),
            new AllocationLine(FundId(ledger, "Food"), 1, AllocationType.Percent, Percent: 50));
        var travelTemplate = await CreateTemplateAsync(travel, "Trips", "EUR",
            new AllocationLine(FundId(ledger, "Flights"), 0, AllocationType.Percent, Percent: 100));
        var ledgerTemplate = await Mediator.Send(new CreateLedgerAllocationTemplateCommand(ledger.Id, "Salary", "EUR", [
            new(home, 0, AllocationType.Percent, Percent: 70),
            new(travel, 1, AllocationType.Percent, Percent: 100)]));

        return new Seed(
            ledger.Id,
            ledger.Accounts.Single().Id,
            travel,
            FundId(ledger, "Food"),
            FundId(ledger, "Rent"),
            FundId(ledger, "Flights"),
            FundId(ledger, "Hotels"),
            homeTemplate,
            travelTemplate,
            ledgerTemplate);
    }

    private async Task<FundTransaction[]> AllocationsOfAsync(Guid accountTransactionId)
        => [.. await EntityStore.GetAsync<FundTransaction>(x => x.AccountTransactionId == accountTransactionId, default)];
}
