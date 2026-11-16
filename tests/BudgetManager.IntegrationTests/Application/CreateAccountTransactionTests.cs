using BudgetManager.Application.Commands;
using BudgetManager.Application.Models;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Models;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Application;

public class CreateAccountTransactionTests(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task CreateAccountTransaction_WhenAllocationIsOmitted_ShouldCreateTransactionWithoutAllocations()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync();
        var accountId = ledger.Accounts.Single().Id;

        // Act
        var id = await Mediator.Send(new CreateAccountTransactionCommand(accountId, new(100, "EUR"), DateTimeOffset.UtcNow, "Salary"));

        // Assert
        var transaction = await EntityStore.GetAsync<AccountTransaction>(id);
        transaction.Value.ShouldBe(new Money(100, "EUR"));
        (await AllocationsOfAsync(id)).ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateAccountTransaction_WhenAllocationUsesTemplate_ShouldAllocateIncomeByTemplate()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");
        var templateId = await CreateTemplateAsync(ledger.Budgets.Single().Id, "Salary", "EUR",
            new AllocationLine(FundId(ledger, "Food"), 0, AllocationType.Fixed, Amount: 100),
            new AllocationLine(FundId(ledger, "Rent"), 1, AllocationType.Percent, Percent: 50));
        var date = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);

        // Act
        var id = await Mediator.Send(new CreateAccountTransactionCommand(
            ledger.Accounts.Single().Id, new(300, "EUR"), date, "Salary", Allocation: new AllocationInput(TemplateId: templateId)));

        // Assert
        var allocations = await AllocationsOfAsync(id);
        allocations.Select(x => (x.FundId, x.Value)).ShouldBe([
            (FundId(ledger, "Food"), new Money(100, "EUR")),
            (FundId(ledger, "Rent"), new Money(100, "EUR"))], ignoreOrder: true);
        allocations.ShouldAllBe(x => x.Date == date && x.Title == "Salary");
    }

    [Fact]
    public async Task CreateAccountTransaction_WhenAllocationHasLines_ShouldAllocateTheLines()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");

        // Act
        var id = await Mediator.Send(new CreateAccountTransactionCommand(
            ledger.Accounts.Single().Id, new(300, "USD"), DateTimeOffset.UtcNow, "Salary",
            Allocation: new AllocationInput(Lines: [
                new(FundId(ledger, "Food"), 0, AllocationType.Fixed, 120),
                new(FundId(ledger, "Rent"), 1, AllocationType.Fixed, 30.5m)])));

        // Assert
        (await AllocationsOfAsync(id)).Select(x => (x.FundId, x.Value)).ShouldBe([
            (FundId(ledger, "Food"), new Money(120, "USD")),
            (FundId(ledger, "Rent"), new Money(30.5m, "USD"))], ignoreOrder: true);
    }

    [Fact]
    public async Task CreateAccountTransaction_WhenLinesExceedIncome_ShouldThrowValidationExceptionAndCreateNothing()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");
        var accountId = ledger.Accounts.Single().Id;
        var command = new CreateAccountTransactionCommand(
            accountId, new(100, "EUR"), DateTimeOffset.UtcNow, "Salary",
            Allocation: new AllocationInput(Lines: [
                new(FundId(ledger, "Food"), 0, AllocationType.Fixed, 60),
                new(FundId(ledger, "Rent"), 1, AllocationType.Fixed, 40.01m)]));

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe("Amount 100 EUR is below the allocation's minimum of 100.01 EUR.");
        (await EntityStore.GetAsync<AccountTransaction>(x => x.AccountId == accountId && x.Title == "Salary", default)).ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateAccountTransaction_WhenTemplateMinimumIsNotReached_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var templateId = await CreateTemplateAsync(ledger.Budgets.Single().Id, "Salary", "EUR", new AllocationLine(FundId(ledger, "Food"), 0, AllocationType.Fixed, Amount: 100));
        var command = new CreateAccountTransactionCommand(
            ledger.Accounts.Single().Id, new(99.99m, "EUR"), DateTimeOffset.UtcNow, "Salary", Allocation: new AllocationInput(TemplateId: templateId));

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe("Amount 99.99 EUR is below the allocation's minimum of 100 EUR.");
    }

    [Fact]
    public async Task CreateAccountTransaction_WhenCustomLinesRepeatAFund_ShouldAllocateEachRule()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");

        // Act
        var id = await Mediator.Send(new CreateAccountTransactionCommand(
            ledger.Accounts.Single().Id, new(200, "EUR"), DateTimeOffset.UtcNow, "Salary",
            Allocation: new AllocationInput(Lines: [
                new(FundId(ledger, "Food"), 0, AllocationType.Fixed, 100),
                new(FundId(ledger, "Food"), 1, AllocationType.Percent, Percent: 10)])));

        // Assert
        (await AllocationsOfAsync(id)).Select(x => x.Value.Amount).ShouldBe([100m, 10m], ignoreOrder: true);
    }

    [Fact]
    public async Task CreateAccountTransaction_WhenCustomLinesAllocateNothing_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var command = new CreateAccountTransactionCommand(
            ledger.Accounts.Single().Id, new(0.01m, "EUR"), DateTimeOffset.UtcNow, "Salary",
            Allocation: new AllocationInput(Lines: [new(FundId(ledger, "Food"), 0, AllocationType.Percent, Percent: 10)]));

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe("Allocation does not allocate anything to funds.");
    }

    [Fact]
    public async Task CreateAccountTransaction_WhenFundBelongsToAnotherLedger_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var otherLedger = await SeedLedgerAsync("Rent");
        var command = new CreateAccountTransactionCommand(
            ledger.Accounts.Single().Id, new(100, "EUR"), DateTimeOffset.UtcNow, "Salary",
            Allocation: new AllocationInput(Lines: [new(FundId(otherLedger, "Rent"), 0, AllocationType.Fixed, 50)]));

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe("Allocated funds must belong to a budget in the account's ledger.");
    }

    [Fact]
    public async Task CreateAccountTransaction_WhenTemplateBelongsToAnotherLedger_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var otherLedger = await SeedLedgerAsync("Rent");
        var templateId = await CreateTemplateAsync(otherLedger.Budgets.Single().Id, "Other", "EUR", new AllocationLine(FundId(otherLedger, "Rent"), 0, AllocationType.Percent, Percent: 10));
        var command = new CreateAccountTransactionCommand(
            ledger.Accounts.Single().Id, new(100, "EUR"), DateTimeOffset.UtcNow, "Salary",
            Allocation: new AllocationInput(TemplateId: templateId));

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe("Allocation template must belong to the account's ledger.");
    }

    [Fact]
    public async Task CreateAccountTransaction_WhenTemplateBelongsToAnotherUser_ShouldThrowAuthorizationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var otherUsersLedger = await SeedLedgerAsync("Rent");
        var templateId = await CreateTemplateAsync(otherUsersLedger.Budgets.Single().Id, "Other", "EUR", new AllocationLine(FundId(otherUsersLedger, "Rent"), 0, AllocationType.Percent, Percent: 10));

        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var command = new CreateAccountTransactionCommand(
            ledger.Accounts.Single().Id, new(100, "EUR"), DateTimeOffset.UtcNow, "Salary",
            Allocation: new AllocationInput(TemplateId: templateId));

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() => Mediator.Send(command));
    }

    [Fact]
    public async Task CreateAccountTransaction_WhenFundBelongsToAnotherUser_ShouldThrowAuthorizationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var otherUsersLedger = await SeedLedgerAsync("Food");

        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync();
        var command = new CreateAccountTransactionCommand(
            ledger.Accounts.Single().Id, new(100, "EUR"), DateTimeOffset.UtcNow, "Salary",
            Allocation: new AllocationInput(Lines: [new(FundId(otherUsersLedger, "Food"), 0, AllocationType.Fixed, 50)]));

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() => Mediator.Send(command));
    }

    [Fact]
    public async Task CreateAccountTransaction_WhenExpenseHasAllocation_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var command = new CreateAccountTransactionCommand(
            ledger.Accounts.Single().Id, new(-100, "EUR"), DateTimeOffset.UtcNow, "Groceries",
            Allocation: new AllocationInput(Lines: [new(FundId(ledger, "Food"), 0, AllocationType.Fixed, 50)]));

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe("Only an income can be allocated to funds.");
    }

    [Fact]
    public async Task CreateAccountTransaction_WhenTemplateIsInAnotherCurrency_ShouldThrowValidationExceptionAndCreateNothing()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var accountId = ledger.Accounts.Single().Id;
        var templateId = await CreateTemplateAsync(ledger.Budgets.Single().Id, "Euros", "EUR", new AllocationLine(FundId(ledger, "Food"), 0, AllocationType.Fixed, Amount: 100));
        var command = new CreateAccountTransactionCommand(
            accountId, new(100, "USD"), DateTimeOffset.UtcNow, "Salary", Allocation: new AllocationInput(TemplateId: templateId));

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe("Allocation template is in EUR, the amount is in USD.");
        (await EntityStore.GetAsync<AccountTransaction>(x => x.AccountId == accountId && x.Title == "Salary", default)).ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateAccountTransaction_WhenExpenseIsPaidFromFund_ShouldLowerFundAndLeaveUnallocatedUnchanged()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var accountId = ledger.Accounts.Single().Id;
        var food = FundId(ledger, "Food");
        await Mediator.Send(new CreateAccountTransactionCommand(accountId, new(200, "EUR"), DateTimeOffset.UtcNow, "Salary",
            Allocation: new AllocationInput(Lines: [new(food, 0, AllocationType.Fixed, 150)])));

        // Act
        var id = await Mediator.Send(new CreateAccountTransactionCommand(accountId, new(-40, "EUR"), DateTimeOffset.UtcNow, "Groceries", FundId: food));

        // Assert
        (await FundBalanceAsync(ledger.Id, food)).ShouldBe(110);
        (await UnallocatedAsync(ledger.Id)).ShouldBe(50);
        (await AllocationsOfAsync(id)).ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            x => x.FundId.ShouldBe(food),
            x => x.Value.ShouldBe(new Money(-40, "EUR")),
            x => x.Title.ShouldBe("Groceries"));
    }

    [Fact]
    public async Task CreateAccountTransaction_WhenExpenseHasNoFund_ShouldLowerUnallocated()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var accountId = ledger.Accounts.Single().Id;
        await Mediator.Send(new CreateAccountTransactionCommand(accountId, new(100, "EUR"), DateTimeOffset.UtcNow, "Salary"));

        // Act
        var id = await Mediator.Send(new CreateAccountTransactionCommand(accountId, new(-40, "EUR"), DateTimeOffset.UtcNow, "Groceries"));

        // Assert
        (await UnallocatedAsync(ledger.Id)).ShouldBe(60);
        (await AllocationsOfAsync(id)).ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateAccountTransaction_WhenFundIsOverspent_ShouldLetItGoNegative()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");

        // Act
        await Mediator.Send(new CreateAccountTransactionCommand(ledger.Accounts.Single().Id, new(-40, "EUR"), DateTimeOffset.UtcNow, "Groceries", FundId: FundId(ledger, "Food")));

        // Assert
        (await FundBalanceAsync(ledger.Id, FundId(ledger, "Food"))).ShouldBe(-40);
    }

    [Fact]
    public async Task CreateAccountTransaction_WhenIncomeIsPaidFromFund_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var command = new CreateAccountTransactionCommand(
            ledger.Accounts.Single().Id, new(100, "EUR"), DateTimeOffset.UtcNow, "Salary", FundId: FundId(ledger, "Food"));

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe("Only an expense can be paid from a fund.");
    }

    [Fact]
    public async Task CreateAccountTransaction_WhenFundIsEmptyGuid_ShouldThrowAuthorizationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var command = new CreateAccountTransactionCommand(
            ledger.Accounts.Single().Id, new(-100, "EUR"), DateTimeOffset.UtcNow, "Groceries", FundId: Guid.Empty);

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() => Mediator.Send(command));
    }

    [Fact]
    public async Task CreateAccountTransaction_WhenExpenseIsAllocatedAndPaidFromFund_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var food = FundId(ledger, "Food");
        var command = new CreateAccountTransactionCommand(
            ledger.Accounts.Single().Id, new(-100, "EUR"), DateTimeOffset.UtcNow, "Groceries",
            Allocation: new AllocationInput(Lines: [new(food, 0, AllocationType.Fixed, 50)]), FundId: food);

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe("A transaction cannot be both allocated and paid from a fund.");
    }

    [Fact]
    public async Task CreateAccountTransaction_WhenPayingFundBelongsToAnotherLedger_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var otherLedger = await SeedLedgerAsync("Rent");
        var command = new CreateAccountTransactionCommand(
            ledger.Accounts.Single().Id, new(-100, "EUR"), DateTimeOffset.UtcNow, "Groceries", FundId: FundId(otherLedger, "Rent"));

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe("Fund must belong to a budget in the account's ledger.");
    }

    [Fact]
    public async Task CreateAccountTransaction_WhenPayingFundBelongsToAnotherUser_ShouldThrowAuthorizationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var otherUsersLedger = await SeedLedgerAsync("Food");

        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync();
        var command = new CreateAccountTransactionCommand(
            ledger.Accounts.Single().Id, new(-100, "EUR"), DateTimeOffset.UtcNow, "Groceries", FundId: FundId(otherUsersLedger, "Food"));

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() => Mediator.Send(command));
    }

    private async Task<FundTransaction[]> AllocationsOfAsync(Guid accountTransactionId)
        => [.. await EntityStore.GetAsync<FundTransaction>(x => x.AccountTransactionId == accountTransactionId, default)];
}
