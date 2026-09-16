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
        var command = new CreateLedgerCommand(string.Empty, null, new(string.Empty, []), []);

        // Act & Assert
        var ex = await Should.ThrowAsync<AuthenticationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe("User ID '00000000-0000-0000-0000-000000000000' is invalid.");
    }

    [Fact]
    public async Task CreateLedger_CreatesLedgerWithBudgetAndAccounts()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();

        var command = new CreateLedgerCommand(
          "[ledger name]",
          "[ledger description]",
          new("[budget name]", [new("[fund name]", 42, 20, AllocationType.Percent, "[fund description]")], "[budget description]"),
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

        var budgets = (await EntityStore.GetAsync<Budget>(x => x.LedgerId == id, default)).ToArray();
        budgets.Length.ShouldBe(1);

        var budget = budgets.First();
        budget.ShouldNotBeNull();
        budget.LedgerId.ShouldBe(id);
        budget.Name.ShouldBe(command.Budget.Name);
        budget.Description.ShouldBe(command.Budget.Description);

        var funds = (await EntityStore.GetAsync<Fund>(x => x.BudgetId == budget.Id, default)).ToArray();
        funds.Length.ShouldBe(1);
        var commandFund = command.Budget.Funds.First();
        var fund = funds.First();

        fund.ShouldNotBeNull();
        fund.Name.ShouldBe(commandFund.Name);
        fund.Description.ShouldBe(commandFund.Description);
        fund.AllocationTemplateSequence.ShouldBe(commandFund.AllocationTemplateSequence);
        fund.AllocationTemplateType.ShouldBe(commandFund.AllocationTemplateType);
        fund.AllocationTemplateValue.ShouldBe(commandFund.AllocationTemplateValue);

        var accounts = (await EntityStore.GetAsync<Account>(x => x.LedgerId == id, default)).OrderBy(x => x.Name).ToArray();
        accounts.Length.ShouldBe(3);

        for (var i = 0; i < 3; i++)
        {
            var account = accounts.ElementAt(1);
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
