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

public class UpdateBudgetTests(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task UpdateBudget_WhenValid_ShouldRenameIt()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var budget = ledger.Budgets.Single();

        // Act
        await Mediator.Send(new UpdateBudgetCommand(budget.Id, "Home", [new(FundId(ledger, "Food"), "Food")]));

        // Assert
        (await Mediator.Send(new GetBudgetQuery(budget.Id))).Name.ShouldBe("Home");
    }

    [Fact]
    public async Task UpdateBudget_WhenFundIsRenamed_ShouldKeepItsIdAndMoney()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var food = FundId(ledger, "Food");
        await IncomeAsync(ledger, 100);
        await AllocationAsync(ledger.Id, (food, 40));

        // Act
        await Mediator.Send(new UpdateBudgetCommand(ledger.Budgets.Single().Id, "Budget", [new(food, "Groceries")]));

        // Assert
        var fund = (await Mediator.Send(new GetBudgetQuery(ledger.Budgets.Single().Id))).Funds.ShouldHaveSingleItem();
        fund.Id.ShouldBe(food);
        fund.Name.ShouldBe("Groceries");
        fund.Balance.GetValueOrDefault("EUR").ShouldBe(40);
    }

    [Fact]
    public async Task UpdateBudget_WhenFundsAreSwappedByName_ShouldRenameBoth()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");
        var food = FundId(ledger, "Food");
        var rent = FundId(ledger, "Rent");

        // Act
        await Mediator.Send(new UpdateBudgetCommand(ledger.Budgets.Single().Id, "Budget", [new(food, "Rent"), new(rent, "Food")]));

        // Assert
        var funds = (await Mediator.Send(new GetBudgetQuery(ledger.Budgets.Single().Id))).Funds.ToDictionary(x => x.Id, x => x.Name);
        funds[food].ShouldBe("Rent");
        funds[rent].ShouldBe("Food");
    }

    [Fact]
    public async Task UpdateBudget_WhenFundHasNoId_ShouldAddIt()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");

        // Act
        await Mediator.Send(new UpdateBudgetCommand(ledger.Budgets.Single().Id, "Budget", [new(FundId(ledger, "Food"), "Food"), new(null, "Repairs")]));

        // Assert
        var funds = (await Mediator.Send(new GetBudgetQuery(ledger.Budgets.Single().Id))).Funds;
        funds.Select(x => x.Name).Order().ShouldBe(["Food", "Repairs"]);
    }

    [Fact]
    public async Task UpdateBudget_WhenFundIsLeftOut_ShouldDeallocateItsMoney()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");
        await IncomeAsync(ledger, 100);
        await AllocationAsync(ledger.Id, (FundId(ledger, "Food"), 20), (FundId(ledger, "Rent"), 30));

        // Act
        await Mediator.Send(new UpdateBudgetCommand(ledger.Budgets.Single().Id, "Budget", [new(FundId(ledger, "Rent"), "Rent")]));

        // Assert
        (await UnallocatedAsync(ledger.Id)).ShouldBe(70);
    }

    [Fact]
    public async Task UpdateBudget_WhenFundIsLeftOut_ShouldDeleteIt()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");
        var food = FundId(ledger, "Food");

        // Act
        await Mediator.Send(new UpdateBudgetCommand(ledger.Budgets.Single().Id, "Budget", [new(FundId(ledger, "Rent"), "Rent")]));

        // Assert
        (await EntityStore.ExistsAsync<Fund>(x => x.Id == food)).ShouldBeFalse();
    }

    [Fact]
    public async Task UpdateBudget_WhenFundWithPaidExpenseIsLeftOut_ShouldDeallocateWhatIsLeftInIt()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");
        var food = FundId(ledger, "Food");
        await IncomeAsync(ledger, 100);
        await AllocationAsync(ledger.Id, (food, 50));
        await Mediator.Send(new CreateAccountTransactionCommand(ledger.Accounts.Single().Id, new(-20, "EUR"), DateTimeOffset.UtcNow, "Shop", FundId: food));

        // Act
        await Mediator.Send(new UpdateBudgetCommand(ledger.Budgets.Single().Id, "Budget", [new(FundId(ledger, "Rent"), "Rent")]));

        // Assert
        (await UnallocatedAsync(ledger.Id)).ShouldBe(80);
    }

    [Fact]
    public async Task UpdateBudget_WhenAllocationHadOnlyTheRemovedFund_ShouldDeleteTheAllocation()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");
        await IncomeAsync(ledger, 100);
        var allocation = await AllocationAsync(ledger.Id, (FundId(ledger, "Food"), 20));

        // Act
        await Mediator.Send(new UpdateBudgetCommand(ledger.Budgets.Single().Id, "Budget", [new(FundId(ledger, "Rent"), "Rent")]));

        // Assert
        (await EntityStore.ExistsAsync<Allocation>(x => x.Id == allocation)).ShouldBeFalse();
    }

    [Fact]
    public async Task UpdateBudget_WhenAllocationHadAnotherFundToo_ShouldKeepTheAllocationWithTheOtherFund()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");
        var rent = FundId(ledger, "Rent");
        await IncomeAsync(ledger, 100);
        var allocation = await AllocationAsync(ledger.Id, (FundId(ledger, "Food"), 20), (rent, 30));

        // Act
        await Mediator.Send(new UpdateBudgetCommand(ledger.Budgets.Single().Id, "Budget", [new(rent, "Rent")]));

        // Assert
        var rows = await EntityStore.GetAsync<FundTransaction>(x => x.AllocationId == allocation);
        rows.ShouldHaveSingleItem().FundId.ShouldBe(rent);
    }

    [Fact]
    public async Task UpdateBudget_WhenTemplateHadOnlyTheRemovedFund_ShouldDeleteTheTemplate()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");
        var budgetId = ledger.Budgets.Single().Id;
        var template = await CreateTemplateAsync(budgetId, "Salary", "EUR", new AllocationLine(FundId(ledger, "Food"), 0, AllocationType.Percent, Percent: 100));

        // Act
        await Mediator.Send(new UpdateBudgetCommand(budgetId, "Budget", [new(FundId(ledger, "Rent"), "Rent")]));

        // Assert
        (await EntityStore.ExistsAsync<AllocationTemplate>(x => x.Id == template)).ShouldBeFalse();
    }

    [Fact]
    public async Task UpdateBudget_WhenTemplateHadAnotherFundToo_ShouldKeepTheTemplateWithoutTheRemovedFund()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");
        var budgetId = ledger.Budgets.Single().Id;
        var rent = FundId(ledger, "Rent");
        await CreateTemplateAsync(budgetId, "Salary", "EUR",
            new AllocationLine(FundId(ledger, "Food"), 0, AllocationType.Percent, Percent: 40),
            new AllocationLine(rent, 1, AllocationType.Percent, Percent: 100));

        // Act
        await Mediator.Send(new UpdateBudgetCommand(budgetId, "Budget", [new(rent, "Rent")]));

        // Assert
        var template = (await Mediator.Send(new GetAllocationTemplatesQuery(budgetId))).ShouldHaveSingleItem();
        template.Lines.ShouldHaveSingleItem().FundId.ShouldBe(rent);
    }

    [Fact]
    public async Task UpdateBudget_WhenNameIsUnchanged_ShouldNotConflictWithItself()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");

        // Act & Assert
        await Should.NotThrowAsync(() => Mediator.Send(new UpdateBudgetCommand(ledger.Budgets.Single().Id, "Budget", [new(FundId(ledger, "Food"), "Food")])));
    }

    [Fact]
    public async Task UpdateBudget_WhenNameIsTakenByAnotherBudget_ShouldThrowConflictException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var otherId = await Mediator.Send(new CreateBudgetCommand(ledger.Id, "Other", [new("Travel")]));
        var travel = (await Mediator.Send(new GetBudgetQuery(otherId))).Funds.Single().Id;

        // Act & Assert
        await Should.ThrowAsync<ConflictException>(() => Mediator.Send(new UpdateBudgetCommand(otherId, "Budget", [new(travel, "Travel")])));
    }

    [Fact]
    public async Task UpdateBudget_WhenFundsAreEmpty_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");

        // Act & Assert
        await Should.ThrowAsync<ValidationException>(() => Mediator.Send(new UpdateBudgetCommand(ledger.Budgets.Single().Id, "Budget", [])));
    }

    [Fact]
    public async Task UpdateBudget_WhenFundNamesRepeat_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");

        // Act & Assert
        await Should.ThrowAsync<ValidationException>(() => Mediator.Send(new UpdateBudgetCommand(
            ledger.Budgets.Single().Id, "Budget", [new(FundId(ledger, "Food"), "Food"), new(null, "Food")])));
    }

    [Fact]
    public async Task UpdateBudget_WhenFundIsListedTwice_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var food = FundId(ledger, "Food");

        // Act & Assert
        await Should.ThrowAsync<ValidationException>(() => Mediator.Send(new UpdateBudgetCommand(
            ledger.Budgets.Single().Id, "Budget", [new(food, "Food"), new(food, "Groceries")])));
    }

    [Fact]
    public async Task UpdateBudget_WhenFundBelongsToAnotherBudget_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var otherId = await Mediator.Send(new CreateBudgetCommand(ledger.Id, "Other", [new("Travel")]));
        var travel = (await Mediator.Send(new GetBudgetQuery(otherId))).Funds.Single().Id;

        // Act & Assert
        await Should.ThrowAsync<ValidationException>(() => Mediator.Send(new UpdateBudgetCommand(
            ledger.Budgets.Single().Id, "Budget", [new(travel, "Travel")])));
    }

    [Fact]
    public async Task UpdateBudget_WhenBudgetBelongsToAnotherUser_ShouldThrowAuthorizationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");

        await MockAuthenticatedUserAsync();

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() => Mediator.Send(new UpdateBudgetCommand(
            ledger.Budgets.Single().Id, "Budget", [new(FundId(ledger, "Food"), "Food")])));
    }

    private Task<Guid> IncomeAsync(LedgerDTO ledger, decimal amount)
        => Mediator.Send(new CreateAccountTransactionCommand(ledger.Accounts.Single().Id, new(amount, "EUR"), DateTimeOffset.UtcNow, "Salary"));

    private async Task<Guid> AllocationAsync(Guid ledgerId, params (Guid FundId, decimal Amount)[] lines)
    {
        var allocation = await EntityStore.CreateAsync(Allocation.Create(
            ledgerId,
            "EUR",
            DateTimeOffset.UtcNow,
            lines.Select(x => new FundAmount(x.FundId, x.Amount))));
        await EntityStore.SaveChangesAsync();
        return allocation.Id;
    }
}
