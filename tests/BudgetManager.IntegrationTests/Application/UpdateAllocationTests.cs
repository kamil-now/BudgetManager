using BudgetManager.Application.Commands;
using BudgetManager.Application.Models;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Models;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Application;

public class UpdateAllocationTests(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    private static readonly DateTimeOffset Date = new(2026, 4, 2, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task UpdateAllocation_WhenAmountChanges_ShouldReplaceRows()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");
        await IncomeAsync(ledger, 300);
        var id = await AllocateAsync(ledger, 100, ("Food", 100));

        // Act
        await Mediator.Send(new UpdateAllocationCommand(ledger.Id, id, "EUR", 150,
            new AllocationInput(Lines: [new(FundId(ledger, "Rent"), 0, AllocationType.Fixed, 150)]), Date));

        // Assert
        var rows = await RowsAsync(id);
        rows.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            x => x.FundId.ShouldBe(FundId(ledger, "Rent")),
            x => x.Value.ShouldBe(new Money(150, "EUR")));
        (await UnallocatedAsync(ledger.Id)).ShouldBe(150);
    }

    [Fact]
    public async Task UpdateAllocation_WhenAmountFitsUnallocatedPlusOldTotal_ShouldSucceed()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        await IncomeAsync(ledger, 100);
        var id = await AllocateAsync(ledger, 80, ("Food", 80));

        // Act
        await Mediator.Send(new UpdateAllocationCommand(ledger.Id, id, "EUR", 100,
            new AllocationInput(Lines: [new(FundId(ledger, "Food"), 0, AllocationType.Fixed, 100)]), Date));

        // Assert
        (await RowsAsync(id)).ShouldHaveSingleItem().Value.ShouldBe(new Money(100, "EUR"));
        (await UnallocatedAsync(ledger.Id)).ShouldBe(0);
    }

    [Fact]
    public async Task UpdateAllocation_WhenAmountExceedsUnallocatedPlusOldTotal_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        await IncomeAsync(ledger, 100);
        var id = await AllocateAsync(ledger, 80, ("Food", 80));
        var command = new UpdateAllocationCommand(ledger.Id, id, "EUR", 100.01m,
            new AllocationInput(Lines: [new(FundId(ledger, "Food"), 0, AllocationType.Fixed, 50)]), Date);

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldStartWith("Amount 100.01 cannot exceed the unallocated 100");
    }

    [Fact]
    public async Task UpdateAllocation_WhenCurrencyChanges_ShouldNotAddTheOldTotalBack()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        await IncomeAsync(ledger, 100);
        await Mediator.Send(new CreateAccountTransactionCommand(ledger.Accounts.Single().Id, new(50, "USD"), DateTimeOffset.UtcNow, "Dollars"));
        var id = await AllocateAsync(ledger, 100, ("Food", 100));
        var command = new UpdateAllocationCommand(ledger.Id, id, "USD", 60,
            new AllocationInput(Lines: [new(FundId(ledger, "Food"), 0, AllocationType.Fixed, 60)]), Date);

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldStartWith("Amount 60 cannot exceed the unallocated 50");
    }

    [Fact]
    public async Task UpdateAllocation_WhenCurrencyChanges_ShouldMoveItToTheNewCurrency()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        await IncomeAsync(ledger, 100);
        await Mediator.Send(new CreateAccountTransactionCommand(ledger.Accounts.Single().Id, new(50, "USD"), DateTimeOffset.UtcNow, "Dollars"));
        var id = await AllocateAsync(ledger, 100, ("Food", 100));

        // Act
        await Mediator.Send(new UpdateAllocationCommand(ledger.Id, id, "USD", 50,
            new AllocationInput(Lines: [new(FundId(ledger, "Food"), 0, AllocationType.Fixed, 50)]), Date));

        // Assert
        (await EntityStore.GetAsync<Allocation>(id)).Currency.ShouldBe("USD");
        (await RowsAsync(id)).ShouldHaveSingleItem().Value.ShouldBe(new Money(50, "USD"));
        (await UnallocatedAsync(ledger.Id, "EUR")).ShouldBe(100);
    }

    [Fact]
    public async Task UpdateAllocation_WhenDateChanges_ShouldRestampAllocationAndRows()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");
        await IncomeAsync(ledger, 300);
        var id = await AllocateAsync(ledger, 100, ("Food", 60), ("Rent", 40));

        // Act
        await Mediator.Send(new UpdateAllocationCommand(ledger.Id, id, "EUR", 100,
            new AllocationInput(Lines: [
                new(FundId(ledger, "Food"), 0, AllocationType.Fixed, 60),
                new(FundId(ledger, "Rent"), 1, AllocationType.Fixed, 40)]), Date));

        // Assert
        (await EntityStore.GetAsync<Allocation>(id)).Date.ShouldBe(Date);
        (await RowsAsync(id)).ShouldAllBe(x => x.Date == Date);
    }

    [Fact]
    public async Task UpdateAllocation_WhenAllocationIsInitial_ShouldStayInitial()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var (ledger, id) = await InitialLedgerAsync();

        // Act
        await Mediator.Send(new UpdateAllocationCommand(ledger.Id, id, "EUR", 100,
            new AllocationInput(Lines: [
                new(FundId(ledger, "Food"), 0, AllocationType.Fixed, 30),
                new(FundId(ledger, "Rent"), 1, AllocationType.Fixed, 70)]), default));

        // Assert
        (await EntityStore.GetAsync<Allocation>(id)).IsInitial.ShouldBeTrue();
        (await RowsAsync(id)).Select(x => x.Value.Amount).ShouldBe([30m, 70m], ignoreOrder: true);
    }

    [Fact]
    public async Task UpdateAllocation_WhenInitialAllocationDateChanges_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var (ledger, id) = await InitialLedgerAsync();
        var command = new UpdateAllocationCommand(ledger.Id, id, "EUR", 100,
            new AllocationInput(Lines: [new(FundId(ledger, "Food"), 0, AllocationType.Fixed, 100)]), Date);

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe("Initial allocation date cannot be changed.");
    }

    [Fact]
    public async Task UpdateAllocation_WhenAllocationUsesTemplate_ShouldSplitByIt()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");
        await IncomeAsync(ledger, 300);
        var templateId = await CreateTemplateAsync(ledger.Budgets.Single().Id, "Salary", "EUR",
            new AllocationLine(FundId(ledger, "Food"), 0, AllocationType.Fixed, Amount: 100),
            new AllocationLine(FundId(ledger, "Rent"), 1, AllocationType.Percent, Percent: 50));
        var id = await AllocateAsync(ledger, 50, ("Food", 50));

        // Act
        await Mediator.Send(new UpdateAllocationCommand(ledger.Id, id, "EUR", 200, new AllocationInput(TemplateId: templateId), Date));

        // Assert
        (await RowsAsync(id)).Select(x => (x.FundId, x.Value)).ShouldBe([
            (FundId(ledger, "Food"), new Money(100, "EUR")),
            (FundId(ledger, "Rent"), new Money(50, "EUR"))], ignoreOrder: true);
    }

    [Fact]
    public async Task UpdateAllocation_WhenAmountIsBelowTemplateMinimum_ShouldThrowValidationExceptionAndKeepRows()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        await IncomeAsync(ledger, 300);
        var templateId = await CreateTemplateAsync(ledger.Budgets.Single().Id, "Salary", "EUR", new AllocationLine(FundId(ledger, "Food"), 0, AllocationType.Fixed, Amount: 100));
        var id = await AllocateAsync(ledger, 50, ("Food", 50));

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(
            new UpdateAllocationCommand(ledger.Id, id, "EUR", 99, new AllocationInput(TemplateId: templateId), Date)));
        ex.Message.ShouldBe("Amount 99 EUR is below the allocation's minimum of 100 EUR.");
        (await RowsAsync(id)).ShouldHaveSingleItem().Value.Amount.ShouldBe(50);
    }

    [Fact]
    public async Task UpdateAllocation_WhenSplitAllocatesNothing_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        await IncomeAsync(ledger, 100);
        var id = await AllocateAsync(ledger, 50, ("Food", 50));
        var command = new UpdateAllocationCommand(ledger.Id, id, "EUR", 0.01m,
            new AllocationInput(Lines: [new(FundId(ledger, "Food"), 0, AllocationType.Percent, Percent: 10)]), Date);

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe("Allocation does not allocate anything to funds.");
        (await RowsAsync(id)).ShouldHaveSingleItem().Value.Amount.ShouldBe(50);
    }

    [Fact]
    public async Task UpdateAllocation_WhenAllocationBelongsToAnotherLedger_ShouldThrowNotFoundException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var otherLedger = await SeedLedgerAsync("Rent");
        await IncomeAsync(otherLedger, 100);
        var id = await AllocateAsync(otherLedger, 50, ("Rent", 50));
        var command = new UpdateAllocationCommand(ledger.Id, id, "EUR", 10,
            new AllocationInput(Lines: [new(FundId(ledger, "Food"), 0, AllocationType.Fixed, 10)]), Date);

        // Act & Assert
        await Should.ThrowAsync<NotFoundException>(() => Mediator.Send(command));
    }

    [Fact]
    public async Task UpdateAllocation_WhenAllocationBelongsToAnotherUser_ShouldThrowAuthorizationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var otherUsersLedger = await SeedLedgerAsync("Food");
        await IncomeAsync(otherUsersLedger, 100);
        var id = await AllocateAsync(otherUsersLedger, 50, ("Food", 50));

        await MockAuthenticatedUserAsync();
        var command = new UpdateAllocationCommand(otherUsersLedger.Id, id, "EUR", 10,
            new AllocationInput(Lines: [new(FundId(otherUsersLedger, "Food"), 0, AllocationType.Fixed, 10)]), Date);

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() => Mediator.Send(command));
    }

    [Fact]
    public async Task UpdateAllocation_WhenFundBelongsToAnotherUser_ShouldThrowAuthorizationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var otherUsersLedger = await SeedLedgerAsync("Food");

        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Rent");
        await IncomeAsync(ledger, 100);
        var id = await AllocateAsync(ledger, 50, ("Rent", 50));
        var command = new UpdateAllocationCommand(ledger.Id, id, "EUR", 10,
            new AllocationInput(Lines: [new(FundId(otherUsersLedger, "Food"), 0, AllocationType.Fixed, 10)]), Date);

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() => Mediator.Send(command));
    }

    [Fact]
    public async Task UpdateAllocation_WhenAllocationIsMissing_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        await IncomeAsync(ledger, 100);
        var id = await AllocateAsync(ledger, 50, ("Food", 50));

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() =>
            Mediator.Send(new UpdateAllocationCommand(ledger.Id, id, "EUR", 10, null!, Date)));
        ex.Message.ShouldBe("Allocation cannot be empty.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateAllocation_WhenAmountIsNotPositive_ShouldThrowValidationException(decimal amount)
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        await IncomeAsync(ledger, 100);
        var id = await AllocateAsync(ledger, 50, ("Food", 50));
        var command = new UpdateAllocationCommand(ledger.Id, id, "EUR", amount,
            new AllocationInput(Lines: [new(FundId(ledger, "Food"), 0, AllocationType.Fixed, 10)]), Date);

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe("Amount must be positive.");
    }

    private async Task IncomeAsync(LedgerDTO ledger, decimal amount)
        => await Mediator.Send(new CreateAccountTransactionCommand(ledger.Accounts.Single().Id, new(amount, "EUR"), DateTimeOffset.UtcNow, "Income"));

    private async Task<Guid> AllocateAsync(LedgerDTO ledger, decimal amount, params (string Fund, decimal Amount)[] lines)
        => await Mediator.Send(new AllocateFundsCommand(ledger.Id, "EUR",
            new AllocationInput(Lines: [.. lines.Select((x, i) => new AllocationLineInput(FundId(ledger, x.Fund), i, AllocationType.Fixed, x.Amount))]),
            Amount: amount));

    private async Task<(LedgerDTO Ledger, Guid AllocationId)> InitialLedgerAsync()
    {
        var ledgerId = await Mediator.Send(new CreateLedgerCommand(
            $"Ledger {Guid.NewGuid()}",
            [new CreateBudgetDTO("Budget", [new CreateFundDTO("Food"), new CreateFundDTO("Rent")])],
            [new CreateAccountDTO(new(100, "EUR"), "Cash")],
            [new InitialAllocationDTO("Budget", "Food", new(100, "EUR"))]));
        var allocation = (await EntityStore.GetAsync<Allocation>(x => x.LedgerId == ledgerId)).Single();
        return (await Mediator.Send(new BudgetManager.Application.Queries.GetLedgerQuery(ledgerId)), allocation.Id);
    }

    private async Task<FundTransaction[]> RowsAsync(Guid allocationId)
        => [.. await EntityStore.GetAsync<FundTransaction>(x => x.AllocationId == allocationId, default)];
}
