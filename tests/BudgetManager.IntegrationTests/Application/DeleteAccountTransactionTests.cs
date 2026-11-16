using BudgetManager.Application.Commands;
using BudgetManager.Application.Models;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Models;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Application;

public class DeleteAccountTransactionTests(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task DeleteAccountTransaction_WhenTransactionIsExpense_ShouldDeleteIt()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync();
        var cash = ledger.Accounts.Single().Id;
        var expense = await Mediator.Send(new CreateAccountTransactionCommand(cash, new(-40, "EUR"), DateTimeOffset.UtcNow, "Groceries"));

        // Act
        await Mediator.Send(new DeleteAccountTransactionCommand(cash, expense));

        // Assert
        (await EntityStore.ExistsAsync<AccountTransaction>(x => x.Id == expense)).ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteAccountTransaction_WhenIncomeIsAllocated_ShouldDeleteItsAllocations()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var cash = ledger.Accounts.Single().Id;
        var template = await CreateTemplateAsync(ledger.Budgets.Single().Id, "Salary", "EUR", new AllocationLine(FundId(ledger, "Food"), 0, AllocationType.Percent, Percent: 100));
        var income = await Mediator.Send(new CreateAccountTransactionCommand(
            cash, new(100, "EUR"), DateTimeOffset.UtcNow, "Salary", Allocation: new AllocationInput(TemplateId: template)));

        // Act
        await Mediator.Send(new DeleteAccountTransactionCommand(cash, income));

        // Assert
        (await EntityStore.ExistsAsync<FundTransaction>(x => x.AccountTransactionId == income)).ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteAccountTransaction_WhenTransactionIsPartOfTransfer_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync();
        var cash = ledger.Accounts.Single().Id;
        var card = await Mediator.Send(new CreateAccountCommand(ledger.Id, new(0, "EUR"), "Card"));
        var transferId = await Mediator.Send(new CreateAccountTransferCommand(cash, card, new(10, "EUR"), DateTimeOffset.UtcNow, "Move"));
        var transfer = await EntityStore.GetAsync<AccountTransfer>(transferId);

        // Act & Assert
        await Should.ThrowAsync<ValidationException>(() => Mediator.Send(new DeleteAccountTransactionCommand(cash, transfer.ExpenseId)));
    }

    [Fact]
    public async Task DeleteAccountTransaction_WhenTransactionIsInitialBalance_ShouldDeleteIt()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync();
        var cash = ledger.Accounts.Single().Id;
        var initialBalance = (await EntityStore.GetAsync<AccountTransaction>(x => x.AccountId == cash)).Single();

        // Act
        await Mediator.Send(new DeleteAccountTransactionCommand(cash, initialBalance.Id));

        // Assert
        (await EntityStore.ExistsAsync<AccountTransaction>(x => x.Id == initialBalance.Id)).ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteAccountTransaction_WhenExpenseIsPaidFromFund_ShouldDeletePayment()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var cash = ledger.Accounts.Single().Id;
        var expense = await Mediator.Send(new CreateAccountTransactionCommand(cash, new(-40, "EUR"), DateTimeOffset.UtcNow, "Groceries", FundId: FundId(ledger, "Food")));

        // Act
        await Mediator.Send(new DeleteAccountTransactionCommand(cash, expense));

        // Assert
        (await EntityStore.ExistsAsync<FundTransaction>(x => x.AccountTransactionId == expense)).ShouldBeFalse();
        (await FundBalanceAsync(ledger.Id, FundId(ledger, "Food"))).ShouldBe(0);
    }

    [Fact]
    public async Task DeleteAccountTransaction_WhenTransactionBelongsToAnotherAccount_ShouldThrowNotFoundException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync();
        var cash = ledger.Accounts.Single().Id;
        var card = await Mediator.Send(new CreateAccountCommand(ledger.Id, new(0, "EUR"), "Card"));
        var expense = await Mediator.Send(new CreateAccountTransactionCommand(cash, new(-40, "EUR"), DateTimeOffset.UtcNow, "Groceries"));

        // Act & Assert
        await Should.ThrowAsync<NotFoundException>(() => Mediator.Send(new DeleteAccountTransactionCommand(card, expense)));
    }

    [Fact]
    public async Task DeleteAccountTransaction_WhenTransactionBelongsToAnotherUser_ShouldThrowAuthorizationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync();
        var cash = ledger.Accounts.Single().Id;
        var expense = await Mediator.Send(new CreateAccountTransactionCommand(cash, new(-40, "EUR"), DateTimeOffset.UtcNow, "Groceries"));

        await MockAuthenticatedUserAsync();

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() => Mediator.Send(new DeleteAccountTransactionCommand(cash, expense)));
    }

    [Fact]
    public async Task DeleteAccountTransaction_WhenTransactionDoesNotExist_ShouldThrowAuthorizationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync();

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() => Mediator.Send(new DeleteAccountTransactionCommand(ledger.Accounts.Single().Id, Guid.NewGuid())));
    }
}
