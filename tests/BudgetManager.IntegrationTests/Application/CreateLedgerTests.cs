using BudgetManager.Application.Commands;
using BudgetManager.Application.Services;
using BudgetManager.Application.Validators;
using BudgetManager.Common.Enums;
using BudgetManager.Domain.Entities;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Application;

public class CreateLedgerTests(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task CreateLedger_CreatesLedgerWithBudgetAndAccount()
    {
        // Arrange
        await BudgetManagerService.CreateAsync(new User()
        {
            Id = Guid.Parse(MockCurrentUserService.MockUserId),
            Name = MockCurrentUserService.MockUserName,
            Email = MockCurrentUserService.MockUserEmail,
            HashedPassword = "0x"
        });
        await BudgetManagerService.SaveChangesAsync();

        var command = new CreateLedgerCommand(
          "[ledger name]",
          "[ledger description]",
          new("[budget name]", [new("[fund name]", 42, 20, AllocationType.Percent, "[fund description]")], "[budget description]"),
          [new(new(123, "EUR"), "[account name]", "[account description]")]);

        // Act
        var id = await Mediator.Send(command);

        // Assert
        var ledger = await BudgetManagerService.GetLedgerAsync(x => x.Id == id, default);
        ledger.ShouldNotBeNull();
        ledger.OwnerId.ToString().ShouldBe(MockCurrentUserService.MockUserId);
        ledger.Name.ShouldBe(command.Name);
        ledger.Description.ShouldBe(command.Description);

        ledger.Budgets.Count.ShouldBe(1);

        var budget = ledger.Budgets.First();
        budget.ShouldNotBeNull();
        budget.OwnerId.ToString().ShouldBe(MockCurrentUserService.MockUserId);
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

        ledger.Accounts.Count.ShouldBe(1);
        var commandAccount = command.Accounts.First();
        var account = ledger.Accounts.First();
        account.ShouldNotBeNull();
        account.OwnerId.ToString().ShouldBe(MockCurrentUserService.MockUserId);
        account.Name.ShouldBe(commandAccount.Name);
        account.Description.ShouldBe(commandAccount.Description);

        account.Incomes.Count.ShouldBe(1);
        var income = account.Incomes.First();
        income.Title.ShouldBe("Initial balance");
        income.Amount.ShouldBeEquivalentTo(commandAccount.InitialBalance);
    }

    [Fact]
    public async Task CreateLedger_WhenAccountInitialBalanceIsNegative_ThrowsException()
    {
        // Arrange
        var command = new CreateLedgerCommand("[ledger name]", null, new("[budget name]", []), [new(new(-1, ""), "[account name]")]);

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));

        ex.Message.ShouldBeEquivalentTo($"InitialBalance of [account name] amount must be greater than zero.");
    }
    // TODO
}
