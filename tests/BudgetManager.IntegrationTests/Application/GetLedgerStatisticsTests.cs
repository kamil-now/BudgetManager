using BudgetManager.Application.Commands;
using BudgetManager.Application.Models;
using BudgetManager.Application.Queries;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Application;

public class GetLedgerStatisticsTests(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task GetLedgerStatistics_WhenTransferIsWithinLedger_ShouldExcludeIt()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var source = await CreateAccountAsync(userId);
        var target = await CreateAccountAsync(userId, source.LedgerId);
        await Mediator.Send(new CreateAccountTransferCommand(source.Id, target.Id, new(100, "PLN"), DateTimeOffset.UtcNow, null));

        // Act
        var statistics = await Mediator.Send(new GetLedgerStatisticsQuery(source.LedgerId, new()));

        // Assert
        statistics.ShouldNotBeNull();
        statistics.TotalIncome.ShouldBeEmpty();
        statistics.TotalExpense.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetLedgerStatistics_WhenTransferLeavesLedger_ShouldCountExpenseInSourceLedger()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var source = await CreateAccountAsync(userId);
        var target = await CreateAccountAsync(userId);
        await Mediator.Send(new CreateAccountTransferCommand(source.Id, target.Id, new(100, "PLN"), DateTimeOffset.UtcNow, null));

        // Act
        var statistics = await Mediator.Send(new GetLedgerStatisticsQuery(source.LedgerId, new()));

        // Assert
        statistics.ShouldNotBeNull();
        statistics.TotalExpense["PLN"].Total.ShouldBe(-100);
        statistics.TotalIncome.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetLedgerStatistics_WhenTransferEntersLedger_ShouldCountIncomeInTargetLedger()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var source = await CreateAccountAsync(userId);
        var target = await CreateAccountAsync(userId);
        await Mediator.Send(new CreateAccountTransferCommand(source.Id, target.Id, new(100, "PLN"), DateTimeOffset.UtcNow, null));

        // Act
        var statistics = await Mediator.Send(new GetLedgerStatisticsQuery(target.LedgerId, new()));

        // Assert
        statistics.ShouldNotBeNull();
        statistics.TotalIncome["PLN"].Total.ShouldBe(100);
        statistics.TotalExpense.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetLedgerStatistics_WhenMoneyIsAllocatedToFunds_ShouldCountOnlyAccountTransactions()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var cash = ledger.Accounts.Single().Id;
        var food = FundId(ledger, "Food");
        await Mediator.Send(new CreateAccountTransactionCommand(cash, new(100, "EUR"), DateTimeOffset.UtcNow, "Salary",
            Allocation: new(Lines: [new(food, 0, AllocationType.Fixed, 30)])));
        await Mediator.Send(new AllocateFundsCommand(ledger.Id, "EUR", new(Lines: [new(food, 0, AllocationType.Fixed, 20)])));

        // Act
        var statistics = await Mediator.Send(new GetLedgerStatisticsQuery(ledger.Id, new()));

        // Assert
        statistics.ShouldNotBeNull();
        statistics.TotalIncome["EUR"].Total.ShouldBe(100);
        statistics.TotalExpense.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 1)]
    public async Task GetLedgerStatistics_WhenInitialBalanceIsRenamed_ShouldStillFilterItByKind(bool includeInitialBalance, int expectedIncomeCurrencies)
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var account = await CreateAccountAsync(userId);
        var initialBalance = AccountTransaction.CreateInitialBalance(account.Id, new(100, "PLN"));
        initialBalance.Update(account.Id, initialBalance.Value, initialBalance.Date, "renamed");
        await EntityStore.CreateAsync(initialBalance);
        await EntityStore.SaveChangesAsync();

        // Act
        var statistics = await Mediator.Send(new GetLedgerStatisticsQuery(account.LedgerId, new() { IncludeInitialBalance = includeInitialBalance }));

        // Assert
        statistics.ShouldNotBeNull();
        statistics.TotalIncome.Count.ShouldBe(expectedIncomeCurrencies);
    }

    [Fact]
    public async Task GetLedgerStatistics_WhenIncomeIsAllocatedToFund_ShouldCountAllocatedAndEnding()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        await IncomeAsync(ledger, 100, January, ("Food", 30));

        // Act
        var statistics = await Mediator.Send(new GetLedgerStatisticsQuery(ledger.Id, new()));

        // Assert
        FlowsOf(statistics, "Food")["EUR"].ShouldBe(new LedgerStatisticsDTO.FundFlow(Allocated: 30, Spent: 0, NetChange: 30, Ending: 30));
    }

    [Fact]
    public async Task GetLedgerStatistics_WhenAllocationIsBeforeFrom_ShouldOnlyCountItInEnding()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        await IncomeAsync(ledger, 100, January, ("Food", 30));

        // Act
        var statistics = await Mediator.Send(new GetLedgerStatisticsQuery(ledger.Id, new() { From = March }));

        // Assert
        FlowsOf(statistics, "Food")["EUR"].ShouldBe(new LedgerStatisticsDTO.FundFlow(Allocated: 0, Spent: 0, NetChange: 0, Ending: 30));
    }

    [Fact]
    public async Task GetLedgerStatistics_WhenAllocationIsAfterTo_ShouldNotCountIt()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        await IncomeAsync(ledger, 100, March, ("Food", 30));

        // Act
        var statistics = await Mediator.Send(new GetLedgerStatisticsQuery(ledger.Id, new() { To = January }));

        // Assert
        FlowsOf(statistics, "Food").ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 100)]
    public async Task GetLedgerStatistics_WhenAllocationIsInitial_ShouldCountItByIncludeInitialBalance(bool includeInitialBalance, decimal expectedAllocated)
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledgerId = await Mediator.Send(new CreateLedgerCommand(
            $"Ledger {Guid.NewGuid()}",
            [new CreateBudgetDTO("Budget", [new CreateFundDTO("Food")])],
            [new CreateAccountDTO(new(100, "EUR"), "Cash")],
            [new InitialAllocationDTO("Budget", "Food", new(100, "EUR"))]));

        // Act
        var statistics = await Mediator.Send(new GetLedgerStatisticsQuery(ledgerId, new() { IncludeInitialBalance = includeInitialBalance }));

        // Assert
        var flow = FlowsOf(statistics, "Food")["EUR"];
        flow.Allocated.ShouldBe(expectedAllocated);
        flow.Ending.ShouldBe(100);
    }

    [Fact]
    public async Task GetLedgerStatistics_WhenExpenseIsPaidFromFund_ShouldCountSpent()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        await IncomeAsync(ledger, 100, January, ("Food", 30));
        await ExpenseAsync(ledger, -12, February, "Food");

        // Act
        var statistics = await Mediator.Send(new GetLedgerStatisticsQuery(ledger.Id, new()));

        // Assert
        FlowsOf(statistics, "Food")["EUR"].ShouldBe(new LedgerStatisticsDTO.FundFlow(Allocated: 30, Spent: -12, NetChange: 18, Ending: 18));
    }

    [Fact]
    public async Task GetLedgerStatistics_WhenPaidExpenseIsBeforeFrom_ShouldOnlyLowerEnding()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        await IncomeAsync(ledger, 100, January, ("Food", 30));
        await ExpenseAsync(ledger, -12, February, "Food");

        // Act
        var statistics = await Mediator.Send(new GetLedgerStatisticsQuery(ledger.Id, new() { From = March }));

        // Assert
        FlowsOf(statistics, "Food")["EUR"].ShouldBe(new LedgerStatisticsDTO.FundFlow(Allocated: 0, Spent: 0, NetChange: 0, Ending: 18));
    }

    [Fact]
    public async Task GetLedgerStatistics_WhenExpenseIsPaidFromFund_ShouldStillCountItInTotalExpense()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        await IncomeAsync(ledger, 100, January, ("Food", 30));
        await ExpenseAsync(ledger, -12, February, "Food");

        // Act
        var statistics = await Mediator.Send(new GetLedgerStatisticsQuery(ledger.Id, new()));

        // Assert
        statistics.ShouldNotBeNull();
        statistics.TotalExpense["EUR"].Total.ShouldBe(-12);
    }

    [Fact]
    public async Task GetLedgerStatistics_WhenBudgetHasSeveralFunds_ShouldSumThemInBudgetTotals()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");
        await IncomeAsync(ledger, 100, January, ("Food", 30), ("Rent", 20));

        // Act
        var statistics = await Mediator.Send(new GetLedgerStatisticsQuery(ledger.Id, new()));

        // Assert
        statistics.ShouldNotBeNull();
        statistics.Budgets.Single().Totals["EUR"].ShouldBe(new LedgerStatisticsDTO.FundFlow(Allocated: 50, Spent: 0, NetChange: 50, Ending: 50));
    }

    [Fact]
    public async Task GetLedgerStatistics_WhenLedgerHasSeveralBudgets_ShouldSumThemInFundTotals()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledgerId = await Mediator.Send(new CreateLedgerCommand(
            $"Ledger {Guid.NewGuid()}",
            [new CreateBudgetDTO("Home", [new CreateFundDTO("Rent")]), new CreateBudgetDTO("Daily", [new CreateFundDTO("Food")])],
            [new CreateAccountDTO(new(0, "EUR"), "Cash")]));
        var ledger = await Mediator.Send(new GetLedgerQuery(ledgerId));
        await IncomeAsync(ledger, 100, January, ("Food", 30), ("Rent", 20));

        // Act
        var statistics = await Mediator.Send(new GetLedgerStatisticsQuery(ledgerId, new()));

        // Assert
        statistics.ShouldNotBeNull();
        statistics.FundTotals["EUR"].ShouldBe(new LedgerStatisticsDTO.FundFlow(Allocated: 50, Spent: 0, NetChange: 50, Ending: 50));
    }

    [Fact]
    public async Task GetLedgerStatistics_WhenFundHasSeveralCurrencies_ShouldKeepThemSeparate()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        await IncomeAsync(ledger, 100, January, ("Food", 30));
        await Mediator.Send(new CreateAccountTransactionCommand(ledger.Accounts.Single().Id, new(50, "USD"), January, "Salary",
            Allocation: new(Lines: [new(FundId(ledger, "Food"), 0, AllocationType.Fixed, 20)])));

        // Act
        var statistics = await Mediator.Send(new GetLedgerStatisticsQuery(ledger.Id, new()));

        // Assert
        var flows = FlowsOf(statistics, "Food");
        flows["EUR"].Allocated.ShouldBe(30);
        flows["USD"].Allocated.ShouldBe(20);
    }

    [Fact]
    public async Task GetLedgerStatistics_WhenFundHasNoTransactions_ShouldListItWithEmptyFlows()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");

        // Act
        var statistics = await Mediator.Send(new GetLedgerStatisticsQuery(ledger.Id, new()));

        // Assert
        statistics.ShouldNotBeNull();
        statistics.Budgets.Single().Funds.Single().Flows.ShouldBeEmpty();
        statistics.Budgets.Single().Totals.ShouldBeEmpty();
        statistics.FundTotals.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetLedgerStatistics_WhenLedgerHasSeveralBudgetsAndFunds_ShouldOrderThemByName()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledgerId = await Mediator.Send(new CreateLedgerCommand(
            $"Ledger {Guid.NewGuid()}",
            [new CreateBudgetDTO("Home", [new CreateFundDTO("Rent"), new CreateFundDTO("Bills")]), new CreateBudgetDTO("Daily", [new CreateFundDTO("Food")])],
            [new CreateAccountDTO(new(0, "EUR"), "Cash")]));

        // Act
        var statistics = await Mediator.Send(new GetLedgerStatisticsQuery(ledgerId, new()));

        // Assert
        statistics.ShouldNotBeNull();
        statistics.Budgets.Select(x => x.Name).ShouldBe(["Daily", "Home"]);
        statistics.Budgets.Last().Funds.Select(x => x.Name).ShouldBe(["Bills", "Rent"]);
    }

    private static readonly DateTimeOffset January = new(2025, 1, 10, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset February = new(2025, 2, 10, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset March = new(2025, 3, 10, 12, 0, 0, TimeSpan.Zero);

    private static Dictionary<string, LedgerStatisticsDTO.FundFlow> FlowsOf(LedgerStatisticsDTO? statistics, string fund)
        => statistics.ShouldNotBeNull().Budgets.SelectMany(x => x.Funds).Single(x => x.Name == fund).Flows;

    private async Task IncomeAsync(LedgerDTO ledger, decimal amount, DateTimeOffset date, params (string Fund, decimal Amount)[] allocations)
        => await Mediator.Send(new CreateAccountTransactionCommand(ledger.Accounts.Single().Id, new(amount, "EUR"), date, "Salary",
            Allocation: new(Lines: [.. allocations.Select((x, i) => new AllocationLineInput(FundId(ledger, x.Fund), i, AllocationType.Fixed, x.Amount))])));

    private async Task ExpenseAsync(LedgerDTO ledger, decimal amount, DateTimeOffset date, string fund)
        => await Mediator.Send(new CreateAccountTransactionCommand(ledger.Accounts.Single().Id, new(amount, "EUR"), date, "Lunch", FundId: FundId(ledger, fund)));

    private async Task<Account> CreateAccountAsync(Guid ownerId, Guid? ledgerId = null)
    {
        if (ledgerId == null)
        {
            var ledger = await EntityStore.CreateAsync(new Ledger { OwnerId = ownerId, Name = $"Ledger {Guid.NewGuid()}" });
            await EntityStore.SaveChangesAsync();
            ledgerId = ledger.Id;
        }

        var account = await EntityStore.CreateAsync(new Account { LedgerId = ledgerId.Value, Name = $"Account {Guid.NewGuid()}" });
        await EntityStore.SaveChangesAsync();
        return account;
    }
}
