using BudgetManager.Application.Commands;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
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
        var command = new CreateLedgerCommand(string.Empty, null, [new(string.Empty, [])], []);

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
          null,
          [
            new("Main budget",
            [
              new("Food", [new("EUR", 0, AllocationType.Fixed, Amount: 600)]),
              new("Rent", [new("EUR", 1, AllocationType.Fixed, Amount: 800)]),
              new("Utilities", [new("EUR", 2, AllocationType.Fixed, Amount: 200)]),
              new("Entertainment", [new("EUR", 3, AllocationType.Percent, Percent: 20)]),
              new("Savings", [new("EUR", 4, AllocationType.Percent, Percent: 80)]),
            ]),
          ],
          [
            new(new(256, "EUR"), "Cash"),
            new(new(2048, "EUR"), "Main Account"),
            new(new(4096, "EUR"), "Savings Account"),
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
          null,
          [
            new("Household", [new("Groceries", [new("EUR", 0, AllocationType.Percent, Percent: 100)])]),
            new("Business", [new("Taxes", [new("EUR", 0, AllocationType.Fixed, Amount: 500)])]),
            new("Travel", [new("Flights", [new("USD", 0, AllocationType.Percent, Percent: 60)])]),
          ],
          [new(new(1000, "EUR"), "Account")]);

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
          "[ledger description]",
          [new("[budget name]", [new("[fund name]", [new("EUR", 42, AllocationType.Percent, Percent: 20)], "[fund description]")], "[budget description]")],
          [
            new(new(123, "EUR"), "[account name 1]", "[account description 1]"),
            new(new(0, "EUR"), "[account name 2]", "[account description 2]"),
            new(new(-123, "EUR"), "[account name 3]", "[account description 3]")
          ]);

        // Act
        var id = await Mediator.Send(command);

        // Assert
        var ledger = await EntityStore.GetAsync<Ledger>(id, default);
        ledger.ShouldNotBeNull();
        ledger.OwnerId.ShouldBe(userId);
        ledger.Name.ShouldBe(command.Name);
        ledger.Description.ShouldBe(command.Description);

        var commandBudget = command.Budgets.First();
        var budgets = (await EntityStore.GetAsync<Budget>(x => x.LedgerId == id, default)).ToArray();
        budgets.Length.ShouldBe(1);

        var budget = budgets.First();
        budget.ShouldNotBeNull();
        budget.LedgerId.ShouldBe(id);
        budget.Name.ShouldBe(commandBudget.Name);
        budget.Description.ShouldBe(commandBudget.Description);

        var funds = (await EntityStore.GetAsync<Fund>(x => x.BudgetId == budget.Id, default)).ToArray();
        funds.Length.ShouldBe(1);
        var commandFund = commandBudget.Funds.First();
        var fund = funds.First();

        fund.ShouldNotBeNull();
        fund.Name.ShouldBe(commandFund.Name);
        fund.Description.ShouldBe(commandFund.Description);

        var template = (await EntityStore.GetAsync<AllocationTemplate>(x => x.BudgetId == budget.Id, default)).ShouldHaveSingleItem();
        template.Currency.ShouldBe("EUR");

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
            account.Description.ShouldBe(commandAccount.Description);

            var transactions = (await EntityStore.GetAsync<AccountTransaction>(x => x.AccountId == account.Id, default)).ToArray();
            transactions.Length.ShouldBe(1);
            var income = transactions.First();
            income.Title.ShouldBe(Constants.InitialBalanceTransactionTitle);
            income.Value.ShouldBeEquivalentTo(commandAccount.InitialBalance);
        }
    }
}
