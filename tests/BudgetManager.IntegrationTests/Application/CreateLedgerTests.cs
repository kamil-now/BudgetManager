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

public class CreateLedgerTests(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task CreateLedger_WhenUserDoesNotExist_ShouldThrowException()
    {
        // Arrange
        MockUnauthenticatedUser();
        var command = new CreateLedgerCommand(string.Empty, [new(string.Empty, [])], []);

        // Act & Assert
        var ex = await Should.ThrowAsync<AuthenticationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe("User ID '00000000-0000-0000-0000-000000000000' is invalid.");
    }

    [Fact]
    public async Task CreateLedger_WithWizardDefaults_ShouldCreateBudgetFundsAndAccounts()
    {
        // Arrange — the exact funds/accounts the new-ledger wizard pre-fills and POSTs.
        await MockAuthenticatedUserAsync();
        var command = new CreateLedgerCommand(
          "Default Ledger",
          [
            new("Main budget",
            [
              new("Food"),
              new("Rent"),
              new("Utilities"),
              new("Entertainment"),
              new("Savings"),
            ],
            [
              new("Default", "EUR",
              [
                new("Food", 0, AllocationType.Fixed, Amount: 600),
                new("Rent", 1, AllocationType.Fixed, Amount: 800),
                new("Utilities", 2, AllocationType.Fixed, Amount: 200),
                new("Entertainment", 3, AllocationType.Percent, Percent: 20),
                new("Savings", 4, AllocationType.Percent, Percent: 100),
              ]),
            ]),
          ],
          [
            new(new(256, "EUR"), "Cash"),
            new(new(2048, "EUR"), "Main Account"),
            new(new(4096, "EUR"), "Savings Account"),
          ],
          [
            new("Main budget", "Food", new(600, "EUR")),
            new("Main budget", "Rent", new(800, "EUR")),
            new("Main budget", "Utilities", new(200, "EUR")),
            new("Main budget", "Entertainment", new(960, "EUR")),
            new("Main budget", "Savings", new(3840, "EUR")),
          ]);

        // Act
        var id = await Mediator.Send(command);

        // Assert
        id.ShouldNotBe(Guid.Empty);
        var budget = (await EntityStore.GetAsync<Budget>(x => x.LedgerId == id, default)).ShouldHaveSingleItem();
        (await EntityStore.GetAsync<Fund>(x => x.BudgetId == budget.Id, default)).Count().ShouldBe(5);
        (await EntityStore.GetAsync<Account>(x => x.LedgerId == id, default)).Count().ShouldBe(3);
    }

    [Fact]
    public async Task CreateLedger_WhenMultipleBudgets_ShouldCreateEveryBudget()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var command = new CreateLedgerCommand(
          "Multi-budget ledger",
          [
            new("Household", [new("Groceries")], [new("Salary", "EUR", [new("Groceries", 0, AllocationType.Percent, Percent: 100)])]),
            new("Business", [new("Taxes")], [new("Invoices", "EUR", [new("Taxes", 0, AllocationType.Fixed, Amount: 500)])]),
            new("Travel", [new("Flights")], [new("Salary", "USD", [new("Flights", 0, AllocationType.Percent, Percent: 60)])]),
          ],
          [new(new(1000, "EUR"), "Account")],
          [new("Household", "Groceries", new(1000, "EUR"))]);

        // Act
        var id = await Mediator.Send(command);

        // Assert
        var budgets = (await EntityStore.GetAsync<Budget>(x => x.LedgerId == id, default)).ToArray();
        budgets.Length.ShouldBe(3);
        budgets.Select(x => x.Name).OrderBy(x => x)
            .ShouldBe(["Business", "Household", "Travel"]);
    }

    [Fact]
    public async Task CreateLedger_CreatesLedgerWithBudgetAndAccounts()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();

        var command = new CreateLedgerCommand(
          "[ledger name]",
          [new("[budget name]", [new("[fund name]")], [new("[template name]", "EUR", [new("[fund name]", 42, AllocationType.Percent, Percent: 20)])])],
          [
            new(new(123, "EUR"), "[account name 1]"),
            new(new(0, "EUR"), "[account name 2]"),
            new(new(-123, "EUR"), "[account name 3]")
          ]);

        // Act
        var id = await Mediator.Send(command);

        // Assert
        var ledger = await EntityStore.GetAsync<Ledger>(id, default);
        ledger.ShouldNotBeNull();
        ledger.OwnerId.ShouldBe(userId);
        ledger.Name.ShouldBe(command.Name);

        var commandBudget = command.Budgets.First();
        var budgets = (await EntityStore.GetAsync<Budget>(x => x.LedgerId == id, default)).ToArray();
        budgets.Length.ShouldBe(1);

        var budget = budgets.First();
        budget.ShouldNotBeNull();
        budget.LedgerId.ShouldBe(id);
        budget.Name.ShouldBe(commandBudget.Name);

        var funds = (await EntityStore.GetAsync<Fund>(x => x.BudgetId == budget.Id, default)).ToArray();
        funds.Length.ShouldBe(1);
        var commandFund = commandBudget.Funds.First();
        var fund = funds.First();

        fund.ShouldNotBeNull();
        fund.Name.ShouldBe(commandFund.Name);

        var template = (await EntityStore.GetAsync<AllocationTemplate>(x => x.BudgetId == budget.Id, default)).ShouldHaveSingleItem();
        template.Currency.ShouldBe("EUR");
        template.Name.ShouldBe("[template name]");

        var line = (await EntityStore.GetAsync<AllocationTemplateLine>(x => x.AllocationTemplateId == template.Id, default)).ShouldHaveSingleItem();
        line.FundId.ShouldBe(fund.Id);
        line.Sequence.ShouldBe(42);
        line.ShouldBeOfType<PercentAllocationTemplateLine>().Percent.ShouldBe(20);

        var accounts = (await EntityStore.GetAsync<Account>(x => x.LedgerId == id, default)).OrderBy(x => x.Name).ToArray();
        accounts.Length.ShouldBe(3);

        foreach (var account in accounts)
        {
            var commandAccount = command.Accounts.FirstOrDefault(x => x.Name == account.Name);
            commandAccount.ShouldNotBeNull();
            account.ShouldNotBeNull();
            account.LedgerId.ShouldBe(id);
            account.Name.ShouldBe(commandAccount.Name);

            var transactions = (await EntityStore.GetAsync<AccountTransaction>(x => x.AccountId == account.Id, default)).ToArray();
            transactions.Length.ShouldBe(1);
            var income = transactions.First();
            income.Title.ShouldBe(Constants.InitialBalanceTransactionTitle);
            income.Value.ShouldBeEquivalentTo(commandAccount.InitialBalance);
        }
    }

    [Fact]
    public async Task CreateLedger_WhenTemplateAllocatesToUnknownFund_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var command = new CreateLedgerCommand(
            "Ledger",
            [new("Budget", [new("Food")], [new("Salary", "EUR", [new("Missing", 0, AllocationType.Percent, Percent: 10)])])],
            [new(new(0, "EUR"), "Cash")]);

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe("Allocation template Salary allocates to unknown fund Missing.");
    }

    [Fact]
    public async Task CreateLedger_WhenTemplateNamesRepeatInBudget_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        CreateAllocationLineDTO[] lines = [new("Food", 0, AllocationType.Percent, Percent: 10)];
        var command = new CreateLedgerCommand(
            "Ledger",
            [new("Budget", [new("Food")], [new("Salary", "EUR", lines),new("Salary", "EUR", lines)])],
            [new(new(0, "EUR"), "Cash")]);

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe("Allocation template names in a budget must be unique.");
    }

    [Fact]
    public async Task CreateLedger_WhenSameTemplateNameIsInTwoBudgets_ShouldCreateBoth()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        CreateAllocationLineDTO[] lines = [new("Food", 0, AllocationType.Percent, Percent: 10)];
        var command = new CreateLedgerCommand(
            "Ledger",
            [
                new("A", [new("Food")], [new("Salary", "EUR", lines)]),
                new("B", [new("Food")], [new("Salary", "EUR", lines)]),
            ],
            [new(new(0, "EUR"), "Cash")]);

        // Act
        var id = await Mediator.Send(command);

        // Assert
        var budgetIds = (await EntityStore.GetAsync<Budget>(x => x.LedgerId == id, default)).Select(x => x.Id).ToArray();
        (await EntityStore.GetAsync<AllocationTemplate>(x => budgetIds.Contains(x.BudgetId), default)).Count().ShouldBe(2);
    }

    private static CreateLedgerCommand LedgerWithInitialBalances(IEnumerable<InitialAllocationDTO>? allocations, params CreateAccountDTO[] accounts) => new(
        $"Ledger {Guid.NewGuid()}",
        [
            new("Household", [new("Food"), new("Savings")]),
            new("Travel", [new("Savings")]),
        ],
        accounts.Length == 0 ? [new(new(100, "EUR"), "Cash")] : accounts,
        allocations);

    private static Balance FundBalance(LedgerDTO ledger, string budget, string fund)
        => ledger.Budgets.Single(x => x.Name == budget).Funds.Single(x => x.Name == fund).Balance;

    [Fact]
    public async Task CreateLedger_WhenInitialAllocationsAreGiven_ShouldBecomeFundBalances()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var command = LedgerWithInitialBalances(
        [
            new("Household", "Food", new(60, "EUR")),
            new("Household", "Savings", new(40, "EUR")),
        ]);

        // Act
        var ledger = await Mediator.Send(new GetLedgerQuery(await Mediator.Send(command)));

        // Assert
        FundBalance(ledger, "Household", "Food").ShouldBe(new Balance { ["EUR"] = 60 }, ignoreOrder: true);
        FundBalance(ledger, "Household", "Savings").ShouldBe(new Balance { ["EUR"] = 40 }, ignoreOrder: true);
        FundBalance(ledger, "Travel", "Savings").ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateLedger_WhenInitialAllocationsAreGiven_ShouldCreateInitialAllocationWithTheirRows()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var command = LedgerWithInitialBalances([new("Household", "Food", new(100, "EUR"))]);

        // Act
        var id = await Mediator.Send(command);

        // Assert
        var allocationId = (await EntityStore.GetAsync<Allocation>(x => x.LedgerId == id)).ShouldHaveSingleItem().Id;
        var allocation = await EntityStore.GetAsync<Allocation>(allocationId, [x => x.Transactions]);
        allocation.ShouldSatisfyAllConditions(
            x => x.IsInitial.ShouldBeTrue(),
            x => x.Currency.ShouldBe("EUR"),
            x => x.Date.ShouldBe(default));
        allocation.Transactions.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            x => x.Value.ShouldBe(new(100, "EUR")),
            x => x.Date.ShouldBe(default),
            x => x.AccountTransactionId.ShouldBeNull());
    }

    [Fact]
    public async Task CreateLedger_WhenInitialAllocationsAreInSeveralCurrencies_ShouldCreateOneInitialAllocationPerCurrency()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var command = LedgerWithInitialBalances(
            [
                new("Household", "Food", new(70, "EUR")),
                new("Household", "Savings", new(30, "EUR")),
                new("Household", "Food", new(20, "USD")),
            ],
            new CreateAccountDTO(new(100, "EUR"), "Cash"),
            new CreateAccountDTO(new(20, "USD"), "Wallet"));

        // Act
        var id = await Mediator.Send(command);

        // Assert
        var allocations = new List<Allocation>();
        foreach (var allocationId in (await EntityStore.GetAsync<Allocation>(x => x.LedgerId == id)).Select(x => x.Id))
        {
            allocations.Add(await EntityStore.GetAsync<Allocation>(allocationId, [x => x.Transactions]));
        }
        allocations.Select(x => (x.Currency, x.Transactions.Count, x.Total)).ShouldBe([("EUR", 2, 100m), ("USD", 1, 20m)], ignoreOrder: true);
    }

    [Fact]
    public async Task CreateLedger_WhenNoInitialAllocationsAreNeeded_ShouldCreateNoAllocation()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var command = LedgerWithInitialBalances(null, new CreateAccountDTO(new(0, "EUR"), "Cash"));

        // Act
        var id = await Mediator.Send(command);

        // Assert
        (await EntityStore.ExistsAsync<Allocation>(x => x.LedgerId == id)).ShouldBeFalse();
    }

    [Fact]
    public async Task CreateLedger_WhenInitialBalancesAreInTwoCurrencies_ShouldAllocateEachCurrency()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var command = LedgerWithInitialBalances(
            [
                new("Household", "Food", new(100, "EUR")),
                new("Household", "Food", new(20, "USD")),
                new("Household", "Savings", new(30, "USD")),
            ],
            new(new(100, "EUR"), "Cash"),
            new(new(50, "USD"), "Dollars"));

        // Act
        var ledger = await Mediator.Send(new GetLedgerQuery(await Mediator.Send(command)));

        // Assert
        FundBalance(ledger, "Household", "Food").ShouldBe(new Balance { ["EUR"] = 100, ["USD"] = 20 }, ignoreOrder: true);
        FundBalance(ledger, "Household", "Savings").ShouldBe(new Balance { ["USD"] = 30 }, ignoreOrder: true);
    }

    [Fact]
    public async Task CreateLedger_WhenAllocationIsSpreadAcrossTwoBudgets_ShouldAllocateToTheNamedBudgetsFunds()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var command = LedgerWithInitialBalances(
        [
            new("Household", "Savings", new(70, "EUR")),
            new("Travel", "Savings", new(30, "EUR")),
        ]);

        // Act
        var ledger = await Mediator.Send(new GetLedgerQuery(await Mediator.Send(command)));

        // Assert
        FundBalance(ledger, "Household", "Savings").ShouldBe(new Balance { ["EUR"] = 70 }, ignoreOrder: true);
        FundBalance(ledger, "Travel", "Savings").ShouldBe(new Balance { ["EUR"] = 30 }, ignoreOrder: true);
    }

    public sealed class InvalidInitialAllocations : TheoryData<InitialAllocationDTO[]?, CreateAccountDTO[], string>
    {
        public InvalidInitialAllocations()
        {
            CreateAccountDTO[] eur100 = [new(new(100, "EUR"), "Cash")];

            Add(null, eur100, "Initial allocations in EUR must add up to 100 (got 0).");
            Add([new("Household", "Food", new(60, "EUR"))], eur100, "Initial allocations in EUR must add up to 100 (got 60).");
            Add([new("Household", "Food", new(60, "EUR")), new("Travel", "Savings", new(60, "EUR"))], eur100, "Initial allocations in EUR must add up to 100 (got 120).");
            Add([new("Travel", "Food", new(100, "EUR"))], eur100, "Initial allocation to Travel / Food does not match any fund of the ledger's budgets.");
            Add([new("Household", "Food", new(100, "EUR")), new("Household", "Food", new(10, "USD"))], eur100, "Nothing to allocate in USD.");
            Add([new("Household", "Food", new(10, "EUR"))], [new(new(-10, "EUR"), "Cash")], "Nothing to allocate in EUR.");
        }
    }

    [Theory]
    [ClassData(typeof(InvalidInitialAllocations))]
    public async Task CreateLedger_WhenInitialAllocationsAreInvalid_ShouldThrowValidationException(InitialAllocationDTO[]? allocations, CreateAccountDTO[] accounts, string expectedError)
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var command = LedgerWithInitialBalances(allocations, accounts);

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe(expectedError);
    }

    [Fact]
    public async Task CreateLedger_WhenInitialAllocationsAreInvalid_ShouldNotCreateTheLedger()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var command = LedgerWithInitialBalances([new("Household", "Food", new(60, "EUR"))]);

        // Act
        await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));

        // Assert
        (await EntityStore.ExistsAsync<Ledger>(x => x.OwnerId == userId, default)).ShouldBeFalse();
    }
}
