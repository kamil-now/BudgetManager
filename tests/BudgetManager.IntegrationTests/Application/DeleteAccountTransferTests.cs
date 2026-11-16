using BudgetManager.Application.Commands;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Entities;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Application;

public class DeleteAccountTransferTests(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task DeleteAccountTransfer_WhenTransferExists_ShouldDeleteItWithBothTransactions()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync();
        var cash = ledger.Accounts.Single().Id;
        var card = await Mediator.Send(new CreateAccountCommand(ledger.Id, new(0, "EUR"), "Card"));
        var transferId = await Mediator.Send(new CreateAccountTransferCommand(cash, card, new(10, "EUR"), DateTimeOffset.UtcNow, "Move"));
        var transfer = await EntityStore.GetAsync<AccountTransfer>(transferId);
        Guid[] legs = [transfer.ExpenseId, transfer.IncomeId];

        // Act
        await Mediator.Send(new DeleteAccountTransferCommand(cash, transferId));

        // Assert
        (await EntityStore.ExistsAsync<AccountTransfer>(x => x.Id == transferId)).ShouldBeFalse();
        (await EntityStore.ExistsAsync<AccountTransaction>(x => legs.Contains(x.Id))).ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteAccountTransfer_WhenTransferIsCurrencyExchange_ShouldDeleteItWithBothTransactions()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync();
        var cash = ledger.Accounts.Single().Id;
        var exchangeId = await Mediator.Send(new CreateCurrencyExchangeCommand(cash, null, new(110, "USD"), new(100, "EUR"), DateTimeOffset.UtcNow, "Exchange"));
        var exchange = await EntityStore.GetAsync<AccountTransfer>(exchangeId);
        Guid[] legs = [exchange.ExpenseId, exchange.IncomeId];

        // Act
        await Mediator.Send(new DeleteAccountTransferCommand(cash, exchangeId));

        // Assert
        (await EntityStore.ExistsAsync<AccountTransfer>(x => x.Id == exchangeId)).ShouldBeFalse();
        (await EntityStore.ExistsAsync<AccountTransaction>(x => legs.Contains(x.Id))).ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteAccountTransfer_WhenAccountIsTheTarget_ShouldDeleteIt()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync();
        var cash = ledger.Accounts.Single().Id;
        var card = await Mediator.Send(new CreateAccountCommand(ledger.Id, new(0, "EUR"), "Card"));
        var transferId = await Mediator.Send(new CreateAccountTransferCommand(cash, card, new(10, "EUR"), DateTimeOffset.UtcNow, "Move"));

        // Act
        await Mediator.Send(new DeleteAccountTransferCommand(card, transferId));

        // Assert
        (await EntityStore.ExistsAsync<AccountTransfer>(x => x.Id == transferId)).ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteAccountTransfer_WhenTransferDoesNotTouchTheAccount_ShouldThrowNotFoundException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync();
        var cash = ledger.Accounts.Single().Id;
        var card = await Mediator.Send(new CreateAccountCommand(ledger.Id, new(0, "EUR"), "Card"));
        var savings = await Mediator.Send(new CreateAccountCommand(ledger.Id, new(0, "EUR"), "Savings"));
        var transferId = await Mediator.Send(new CreateAccountTransferCommand(cash, card, new(10, "EUR"), DateTimeOffset.UtcNow, "Move"));

        // Act & Assert
        await Should.ThrowAsync<NotFoundException>(() => Mediator.Send(new DeleteAccountTransferCommand(savings, transferId)));
    }

    [Fact]
    public async Task DeleteAccountTransfer_WhenTransferBelongsToAnotherUser_ShouldThrowAuthorizationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync();
        var cash = ledger.Accounts.Single().Id;
        var card = await Mediator.Send(new CreateAccountCommand(ledger.Id, new(0, "EUR"), "Card"));
        var transferId = await Mediator.Send(new CreateAccountTransferCommand(cash, card, new(10, "EUR"), DateTimeOffset.UtcNow, "Move"));

        await MockAuthenticatedUserAsync();

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() => Mediator.Send(new DeleteAccountTransferCommand(cash, transferId)));
    }
}
