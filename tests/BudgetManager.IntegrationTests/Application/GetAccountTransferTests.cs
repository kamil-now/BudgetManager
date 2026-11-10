using BudgetManager.Application.Commands;
using BudgetManager.Application.Queries;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Models;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Application;

public class GetAccountTransferTests(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task GetAccountTransfer_WhenTransfer_ShouldReturnTargetAndNoReceived()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var source = await CreateAccountAsync(userId);
        var target = await CreateAccountAsync(userId, source.LedgerId);
        var transferId = await Mediator.Send(
            new CreateAccountTransferCommand(source.Id, target.Id, new(100, "PLN"), DateTimeOffset.UtcNow, "Move"));

        // Act
        var result = await Mediator.Send(new GetAccountTransferQuery(source.Id, transferId));

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(transferId);
        result.AccountId.ShouldBe(source.Id);
        result.TargetAccountId.ShouldBe(target.Id);
        result.Value.ShouldBe(new Money(100, "PLN"));
        result.Received.ShouldBeNull();
        result.Title.ShouldBe("Move");
        result.TargetAccountName.ShouldBe(target.Name);
    }

    [Fact]
    public async Task GetAccountTransfer_WhenQueriedThroughTargetAccount_ShouldReturnTransfer()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var source = await CreateAccountAsync(userId);
        var target = await CreateAccountAsync(userId, source.LedgerId);
        var transferId = await Mediator.Send(
            new CreateAccountTransferCommand(source.Id, target.Id, new(100, "PLN"), DateTimeOffset.UtcNow, "Move"));

        // Act
        var result = await Mediator.Send(new GetAccountTransferQuery(target.Id, transferId));

        // Assert
        result.ShouldNotBeNull();
        result.AccountId.ShouldBe(source.Id);
        result.TargetAccountId.ShouldBe(target.Id);
    }

    [Fact]
    public async Task GetAccountTransfer_WhenCurrencyExchange_ShouldReturnSoldAndReceived()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var account = await CreateAccountAsync(userId);
        // Buy 90 EUR, sell 100 USD (stored as expense -100 USD, income +90 EUR).
        var exchangeId = await Mediator.Send(
            new CreateCurrencyExchangeCommand(account.Id, null, new(90, "EUR"), new(100, "USD"), DateTimeOffset.UtcNow, null));

        // Act
        var result = await Mediator.Send(new GetAccountTransferQuery(account.Id, exchangeId));

        // Assert
        result.ShouldNotBeNull();
        result.Value.ShouldBe(new Money(100, "USD"));
        result.Received.ShouldBe(new Money(90, "EUR"));
    }

    [Fact]
    public async Task GetAccountTransfer_WhenTransferBelongsToAnotherUser_ShouldThrowAuthorizationException()
    {
        // Arrange
        var otherUserId = await MockAuthenticatedUserAsync();
        var source = await CreateAccountAsync(otherUserId);
        var target = await CreateAccountAsync(otherUserId, source.LedgerId);
        var transferId = await Mediator.Send(
            new CreateAccountTransferCommand(source.Id, target.Id, new(100, "PLN"), DateTimeOffset.UtcNow, null));

        var userId = await MockAuthenticatedUserAsync();
        var ownAccount = await CreateAccountAsync(userId);

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() =>
            Mediator.Send(new GetAccountTransferQuery(ownAccount.Id, transferId)));
    }

    [Fact]
    public async Task GetAccountTransfer_WhenTransferDoesNotInvolveAccount_ShouldThrowNotFoundException()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var account = await CreateAccountAsync(userId);
        var source = await CreateAccountAsync(userId, account.LedgerId);
        var target = await CreateAccountAsync(userId, account.LedgerId);
        var transferId = await Mediator.Send(
            new CreateAccountTransferCommand(source.Id, target.Id, new(100, "PLN"), DateTimeOffset.UtcNow, null));

        // Act & Assert
        await Should.ThrowAsync<NotFoundException>(() =>
            Mediator.Send(new GetAccountTransferQuery(account.Id, transferId)));
    }

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
