using BudgetManager.Application.Commands;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain;
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
        var ledger = await LedgerReader.ReadAsync(id, default);
        ledger.ShouldNotBeNull();
        ledger.OwnerId.ShouldBe(userId);
        ledger.Name.ShouldBe(command.Name);
        ledger.Description.ShouldBe(command.Description);

        ledger.Budgets.Count.ShouldBe(1);

        var budget = ledger.Budgets.First();
        budget.ShouldNotBeNull();
        budget.LedgerId.ShouldBe(id);
        budget.Name.ShouldBe(command.Budget.Name);
        budget.Description.ShouldBe(command.Budget.Description);

        budget.Funds.Count.ShouldBe(1);
        var commandFund = command.Budget.Funds.First();
        var fund = budget.Funds.First();

        fund.ShouldNotBeNull();
        fund.Name.ShouldBe(commandFund.Name);
        fund.Description.ShouldBe(commandFund.Description);
        fund.AllocationTemplateSequence.ShouldBe(commandFund.AllocationTemplateSequence);
        fund.AllocationTemplateType.ShouldBe(commandFund.AllocationTemplateType);
        fund.AllocationTemplateValue.ShouldBe(commandFund.AllocationTemplateValue);

        ledger.Accounts.Count.ShouldBe(3);

        for (var i = 0; i < 3; i++)
        {
            var account = ledger.Accounts.ElementAt(1);
            var commandAccount = command.Accounts.FirstOrDefault(x => x.Name == account.Name);
            commandAccount.ShouldNotBeNull();
            account.ShouldNotBeNull();
            account.LedgerId.ShouldBe(id);
            account.Name.ShouldBe(commandAccount.Name);
            account.Description.ShouldBe(commandAccount.Description);

            account.Transactions.Count.ShouldBe(1);
            var income = account.Transactions.First();
            income.Title.ShouldBe(Constants.InitialBalanceTransactionTitle);
            income.Value.ShouldBeEquivalentTo(commandAccount.InitialBalance);
        }
    }
}
