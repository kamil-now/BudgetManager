using BudgetManager.Application.Commands;
using BudgetManager.Application.Models;
using BudgetManager.Application.Queries;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Models;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Application;

public class AllocateFundsTests(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task AllocateFunds_WhenTemplateIsUsedWithoutAmount_ShouldSplitAllUnallocatedMoney()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");
        await IncomeAsync(ledger, 300);
        var templateId = await SetTemplateAsync(ledger);

        // Act
        var id = await Mediator.Send(new AllocateFundsCommand(ledger.Id, "EUR", new AllocationInput(TemplateId: templateId)));

        // Assert
        (await FundTransactionsAsync(id)).Select(x => (x.FundId, x.Value)).ShouldBe([
            (FundId(ledger, "Food"), new Money(100, "EUR")),
            (FundId(ledger, "Rent"), new Money(100, "EUR"))], ignoreOrder: true);
    }

    [Fact]
    public async Task AllocateFunds_WhenAmountIsGiven_ShouldSplitOnlyTheAmount()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");
        await IncomeAsync(ledger, 300);
        var templateId = await SetTemplateAsync(ledger);

        // Act
        var id = await Mediator.Send(new AllocateFundsCommand(ledger.Id, "EUR", new AllocationInput(TemplateId: templateId), Amount: 200));

        // Assert
        (await FundTransactionsAsync(id)).Select(x => (x.FundId, x.Value)).ShouldBe([
            (FundId(ledger, "Food"), new Money(100, "EUR")),
            (FundId(ledger, "Rent"), new Money(50, "EUR"))], ignoreOrder: true);
    }

    [Fact]
    public async Task AllocateFunds_WhenAllocationHasLines_ShouldCreateOneAllocationWithRowsOfTheLines()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");
        await IncomeAsync(ledger, 300);
        var allocation = new AllocationInput(Lines: [
            new(FundId(ledger, "Food"), 0, AllocationType.Fixed, 120),
            new(FundId(ledger, "Rent"), 1, AllocationType.Fixed, 30.5m)]);

        // Act
        var id = await Mediator.Send(new AllocateFundsCommand(ledger.Id, "EUR", allocation));

        // Assert
        var transactions = await FundTransactionsAsync(id);
        transactions.Select(x => (x.FundId, x.Value)).ShouldBe([
            (FundId(ledger, "Food"), new Money(120, "EUR")),
            (FundId(ledger, "Rent"), new Money(30.5m, "EUR"))], ignoreOrder: true);
        transactions.ShouldAllBe(x => x.AccountTransactionId == null);
    }

    [Fact]
    public async Task AllocateFunds_WhenCalled_ShouldReturnIdOfTheAllocationOfTheLedger()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        await IncomeAsync(ledger, 100);

        // Act
        var id = await Mediator.Send(new AllocateFundsCommand(ledger.Id, "EUR", new AllocationInput(Lines: [new(FundId(ledger, "Food"), 0, AllocationType.Fixed, 10)])));

        // Assert
        var allocation = await EntityStore.GetAsync<Allocation>(id);
        allocation.ShouldSatisfyAllConditions(
            x => x.LedgerId.ShouldBe(ledger.Id),
            x => x.Currency.ShouldBe("EUR"),
            x => x.IsInitial.ShouldBeFalse());
    }

    [Fact]
    public async Task AllocateFunds_WhenSeveralFundsAreAllocated_ShouldGroupTheirRowsInOneAllocation()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");
        await IncomeAsync(ledger, 300);
        var allocation = new AllocationInput(Lines: [
            new(FundId(ledger, "Food"), 0, AllocationType.Fixed, 120),
            new(FundId(ledger, "Rent"), 1, AllocationType.Fixed, 30)]);

        // Act
        await Mediator.Send(new AllocateFundsCommand(ledger.Id, "EUR", allocation));

        // Assert
        var created = (await EntityStore.GetAsync<Allocation>(x => x.LedgerId == ledger.Id)).ShouldHaveSingleItem();
        (await FundTransactionsAsync(created.Id)).Length.ShouldBe(2);
    }

    [Fact]
    public async Task AllocateFunds_WhenInitialBalancesAreAllocated_ShouldAllocateOnlyTheRest()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledgerId = await Mediator.Send(new CreateLedgerCommand(
            $"Ledger {Guid.NewGuid()}",
            [new CreateBudgetDTO("Budget", [new CreateFundDTO("Food")])],
            [new CreateAccountDTO(new(100, "EUR"), "Cash")],
            [new InitialAllocationDTO("Budget", "Food", new(100, "EUR"))]));
        var ledger = await Mediator.Send(new GetLedgerQuery(ledgerId));
        await IncomeAsync(ledger, 50);

        // Act
        var id = await Mediator.Send(new AllocateFundsCommand(ledger.Id, "EUR", new AllocationInput(Lines: [new(FundId(ledger, "Food"), 0, AllocationType.Fixed, 50)])));

        // Assert
        (await FundTransactionsAsync(id)).ShouldHaveSingleItem().Value.ShouldBe(new Money(50, "EUR"));
    }

    [Fact]
    public async Task AllocateFunds_WhenAmountExceedsUnallocated_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        await IncomeAsync(ledger, 100);
        var command = new AllocateFundsCommand(ledger.Id, "EUR", new AllocationInput(Lines: [new(FundId(ledger, "Food"), 0, AllocationType.Fixed, 50)]), Amount: 100.01m);

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldStartWith("Amount 100.01 cannot exceed the unallocated 100");
    }

    [Fact]
    public async Task AllocateFunds_WhenFixedLinesExceedAmount_ShouldThrowValidationExceptionAndAllocateNothing()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");
        await IncomeAsync(ledger, 300);
        var command = new AllocateFundsCommand(ledger.Id, "EUR", new AllocationInput(Lines: [
            new(FundId(ledger, "Food"), 0, AllocationType.Fixed, 60),
            new(FundId(ledger, "Rent"), 1, AllocationType.Fixed, 50)]), Amount: 100);

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe("Amount 100 EUR is below the allocation's minimum of 110 EUR.");
        (await EntityStore.GetAsync<Allocation>(x => x.LedgerId == ledger.Id)).ShouldBeEmpty();
    }

    [Fact]
    public async Task AllocateFunds_WhenTemplateMinimumExceedsUnallocatedAndAmountIsOmitted_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");
        await IncomeAsync(ledger, 99);
        var templateId = await SetTemplateAsync(ledger);

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(new AllocateFundsCommand(ledger.Id, "EUR", new AllocationInput(TemplateId: templateId))));
        ex.Message.ShouldBe("Amount 99 EUR is below the allocation's minimum of 100 EUR.");
    }

    [Fact]
    public async Task AllocateFunds_WhenAmountEqualsTemplateMinimum_ShouldFundEveryFixedStep()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");
        await IncomeAsync(ledger, 300);
        var templateId = await SetTemplateAsync(ledger);

        // Act
        var id = await Mediator.Send(new AllocateFundsCommand(ledger.Id, "EUR", new AllocationInput(TemplateId: templateId), Amount: 100));

        // Assert
        (await FundTransactionsAsync(id)).ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            x => x.FundId.ShouldBe(FundId(ledger, "Food")),
            x => x.Value.ShouldBe(new Money(100, "EUR")));
    }

    [Fact]
    public async Task AllocateFunds_WhenCustomLinesRepeatAFundWithPercents_ShouldSplitLikeATemplate()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");
        await IncomeAsync(ledger, 300);
        var command = new AllocateFundsCommand(ledger.Id, "EUR", new AllocationInput(Lines: [
            new(FundId(ledger, "Rent"), 0, AllocationType.Percent, Percent: 50),
            new(FundId(ledger, "Food"), 1, AllocationType.Fixed, 50),
            new(FundId(ledger, "Food"), 2, AllocationType.Percent, Percent: 50)]), Amount: 200);

        // Act
        var id = await Mediator.Send(command);

        // Assert
        (await FundTransactionsAsync(id)).Select(x => (x.FundId, x.Value)).ShouldBe([
            (FundId(ledger, "Rent"), new Money(100, "EUR")),
            (FundId(ledger, "Food"), new Money(50, "EUR")),
            (FundId(ledger, "Food"), new Money(25, "EUR"))], ignoreOrder: true);
    }

    [Fact]
    public async Task AllocateFunds_WhenCustomLinesAllocateNothing_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        await IncomeAsync(ledger, 0.01m);
        var command = new AllocateFundsCommand(ledger.Id, "EUR", new AllocationInput(Lines: [new(FundId(ledger, "Food"), 0, AllocationType.Percent, Percent: 10)]));

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe("Allocation does not allocate anything to funds.");
    }

    [Fact]
    public async Task AllocateFunds_WhenEverythingIsAllocated_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        await IncomeAsync(ledger, 100);
        var allocation = new AllocationInput(Lines: [new(FundId(ledger, "Food"), 0, AllocationType.Fixed, 100)]);
        await Mediator.Send(new AllocateFundsCommand(ledger.Id, "EUR", allocation));

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(new AllocateFundsCommand(ledger.Id, "EUR", allocation)));
        ex.Message.ShouldBe("Nothing to allocate in EUR.");
    }

    [Fact]
    public async Task AllocateFunds_WhenUnallocatedIsNegative_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        await Mediator.Send(new CreateAccountTransactionCommand(ledger.Accounts.Single().Id, new(-100, "EUR"), DateTimeOffset.UtcNow, "Expense"));
        var command = new AllocateFundsCommand(ledger.Id, "EUR", new AllocationInput(Lines: [new(FundId(ledger, "Food"), 0, AllocationType.Fixed, 10)]));

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe("Nothing to allocate in EUR.");
    }

    [Fact]
    public async Task AllocateFunds_WhenTemplateIsInAnotherCurrency_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        await IncomeAsync(ledger, 100);
        var templateId = await CreateTemplateAsync(ledger.Budgets.Single().Id, "Dollars", "USD", new AllocationLine(FundId(ledger, "Food"), 0, AllocationType.Percent, Percent: 10));
        var command = new AllocateFundsCommand(ledger.Id, "EUR", new AllocationInput(TemplateId: templateId));

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe("Allocation template is in USD, the amount is in EUR.");
    }

    [Fact]
    public async Task AllocateFunds_WhenTemplateAllocatesNothing_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        await IncomeAsync(ledger, 0.01m);
        var templateId = await CreateTemplateAsync(ledger.Budgets.Single().Id, "Tiny", "EUR", new AllocationLine(FundId(ledger, "Food"), 0, AllocationType.Percent, Percent: 10));
        var command = new AllocateFundsCommand(ledger.Id, "EUR", new AllocationInput(TemplateId: templateId));

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe("Allocation does not allocate anything to funds.");
    }

    [Fact]
    public async Task AllocateFunds_WhenTemplateBelongsToAnotherLedgerOfTheUser_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var otherLedger = await SeedLedgerAsync("Rent");
        await IncomeAsync(ledger, 100);
        var templateId = await SetTemplateAsync(otherLedger, "Rent");
        var command = new AllocateFundsCommand(ledger.Id, "EUR", new AllocationInput(TemplateId: templateId));

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe("Allocation template must belong to the account's ledger.");
    }

    [Fact]
    public async Task AllocateFunds_WhenTemplateDoesNotExist_ShouldThrowAuthorizationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        await IncomeAsync(ledger, 100);
        var command = new AllocateFundsCommand(ledger.Id, "EUR", new AllocationInput(TemplateId: Guid.NewGuid()));

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() => Mediator.Send(command));
    }

    [Fact]
    public async Task AllocateFunds_WhenLedgerBelongsToAnotherUser_ShouldThrowAuthorizationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var otherUsersLedger = await SeedLedgerAsync("Food");
        await IncomeAsync(otherUsersLedger, 100);

        await MockAuthenticatedUserAsync();
        var command = new AllocateFundsCommand(otherUsersLedger.Id, "EUR", new AllocationInput(Lines: [new(FundId(otherUsersLedger, "Food"), 0, AllocationType.Fixed, 10)]));

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() => Mediator.Send(command));
    }

    [Fact]
    public async Task AllocateFunds_WhenFundBelongsToAnotherUser_ShouldThrowAuthorizationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var otherUsersLedger = await SeedLedgerAsync("Food");

        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync();
        await IncomeAsync(ledger, 100);
        var command = new AllocateFundsCommand(ledger.Id, "EUR", new AllocationInput(Lines: [new(FundId(otherUsersLedger, "Food"), 0, AllocationType.Fixed, 10)]));

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() => Mediator.Send(command));
    }

    [Fact]
    public async Task AllocateFunds_WhenDateIsOmitted_ShouldDateAllocationsTodayAtUtcMidnight()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        await IncomeAsync(ledger, 100);
        var today = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);

        // Act
        var id = await Mediator.Send(new AllocateFundsCommand(ledger.Id, "EUR", new AllocationInput(Lines: [new(FundId(ledger, "Food"), 0, AllocationType.Fixed, 10)])));

        // Assert
        (await FundTransactionsAsync(id)).ShouldHaveSingleItem().Date.ShouldBe(today);
    }

    [Fact]
    public async Task AllocateFunds_WhenDateIsGiven_ShouldUseIt()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        await IncomeAsync(ledger, 100);
        var date = new DateTimeOffset(2026, 4, 2, 0, 0, 0, TimeSpan.Zero);

        // Act
        var id = await Mediator.Send(new AllocateFundsCommand(
            ledger.Id, "EUR", new AllocationInput(Lines: [new(FundId(ledger, "Food"), 0, AllocationType.Fixed, 10)]), Date: date));

        // Assert
        (await FundTransactionsAsync(id)).ShouldHaveSingleItem().Date.ShouldBe(date);
    }

    public sealed class InvalidCommands : TheoryData<string, decimal?, string>
    {
        public InvalidCommands()
        {
            Add("EU", null, "Currency value 'EU' is not a valid currency code.");
            Add("EUR", 0, "Amount must be positive.");
            Add("EUR", -1, "Amount must be positive.");
            Add("EUR", 1.234m, $"Amount value '{1.234m}' cannot have more than {Constants.MoneyDecimalPlaces} decimal places.");
        }
    }

    [Theory]
    [ClassData(typeof(InvalidCommands))]
    public async Task AllocateFunds_WhenCommandIsInvalid_ShouldThrowValidationException(string currency, decimal? amount, string expectedError)
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var command = new AllocateFundsCommand(ledger.Id, currency, new AllocationInput(Lines: [new(FundId(ledger, "Food"), 0, AllocationType.Fixed, 10)]), amount);

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe(expectedError);
    }

    [Fact]
    public async Task AllocateFunds_WhenAllocationIsMissing_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(new AllocateFundsCommand(ledger.Id, "EUR", null!)));
        ex.Message.ShouldBe("Allocation cannot be empty.");
    }

    private async Task IncomeAsync(LedgerDTO ledger, decimal amount)
        => await Mediator.Send(new CreateAccountTransactionCommand(ledger.Accounts.Single().Id, new(amount, "EUR"), DateTimeOffset.UtcNow, "Income"));

    /// <summary>
    /// Food: fixed 100, then Rent: 50 % of the rest (or only <paramref name="fund"/>: 100 %).
    /// </summary>
    private Task<Guid> SetTemplateAsync(LedgerDTO ledger, string? fund = null)
        => CreateTemplateAsync(ledger.Budgets.Single().Id, "Template", "EUR", fund is null
            ? [
                new AllocationLine(FundId(ledger, "Food"), 0, AllocationType.Fixed, Amount: 100),
                new AllocationLine(FundId(ledger, "Rent"), 1, AllocationType.Percent, Percent: 50)]
            : [new AllocationLine(FundId(ledger, fund), 0, AllocationType.Percent, Percent: 100)]);

    private async Task<FundTransaction[]> FundTransactionsAsync(Guid allocationId)
        => [.. await EntityStore.GetAsync<FundTransaction>(x => x.AllocationId == allocationId, default)];
}
