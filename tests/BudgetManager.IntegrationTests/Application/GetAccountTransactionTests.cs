using BudgetManager.Application.Commands;
using BudgetManager.Application.Queries;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Models;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Application;

public class GetAccountTransactionTests(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task GetAccountTransaction_WhenTransactionBelongsToUser_ShouldReturnTransaction()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var account = await CreateAccountAsync(userId);
        var transaction = await EntityStore.CreateAsync(
            AccountTransaction.Create(account.Id, new Money(50, "PLN"), DateTimeOffset.UtcNow, "Groceries", "weekly", ["food"]));
        await EntityStore.SaveChangesAsync();

        // Act
        var result = await Mediator.Send(new GetAccountTransactionQuery(account.Id, transaction.Id));

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(transaction.Id);
        result.AccountId.ShouldBe(account.Id);
        result.AccountName.ShouldBe(account.Name);
        result.Value.ShouldBe(new Money(50, "PLN"));
        result.Title.ShouldBe("Groceries");
        result.Tags.ShouldBe(["food"]);
    }

    [Fact]
    public async Task GetAccountTransaction_WhenTransactionBelongsToAnotherUser_ShouldThrowAuthorizationException()
    {
        // Arrange
        var otherUserId = await MockAuthenticatedUserAsync();
        var account = await CreateAccountAsync(otherUserId);
        var transaction = await EntityStore.CreateAsync(
            AccountTransaction.Create(account.Id, new Money(50, "PLN"), DateTimeOffset.UtcNow, "Groceries"));
        await EntityStore.SaveChangesAsync();

        await MockAuthenticatedUserAsync();

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() =>
            Mediator.Send(new GetAccountTransactionQuery(account.Id, transaction.Id)));
    }

    [Fact]
    public async Task GetAccountTransaction_WhenTransactionBelongsToAnotherAccount_ShouldThrowNotFoundException()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var account = await CreateAccountAsync(userId);
        var otherAccount = await CreateAccountAsync(userId);
        var transaction = await EntityStore.CreateAsync(
            AccountTransaction.Create(otherAccount.Id, new Money(50, "PLN"), DateTimeOffset.UtcNow, "Groceries"));
        await EntityStore.SaveChangesAsync();

        // Act & Assert
        await Should.ThrowAsync<NotFoundException>(() =>
            Mediator.Send(new GetAccountTransactionQuery(account.Id, transaction.Id)));
    }

    [Fact]
    public async Task GetAccountTransaction_WhenTransactionIsOutgoingTransfer_ShouldThrowNotFoundException()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var source = await CreateAccountAsync(userId);
        var target = await CreateAccountAsync(userId, source.LedgerId);
        await Mediator.Send(new CreateAccountTransferCommand(source.Id, target.Id, new(100, "PLN"), DateTimeOffset.UtcNow, null));
        var transaction =(await EntityStore.GetAsync<AccountTransaction>(x => x.AccountId == source.Id, default)).Single();

        // Act & Assert
        await Should.ThrowAsync<NotFoundException>(() =>
            Mediator.Send(new GetAccountTransactionQuery(source.Id, transaction.Id)));
    }

    [Fact]
    public async Task GetAccountTransaction_WhenTransactionIsIncomingTransfer_ShouldThrowNotFoundException()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var source = await CreateAccountAsync(userId);
        var target = await CreateAccountAsync(userId, source.LedgerId);
        await Mediator.Send(new CreateAccountTransferCommand(source.Id, target.Id, new(100, "PLN"), DateTimeOffset.UtcNow, null));
        var transaction =(await EntityStore.GetAsync<AccountTransaction>(x => x.AccountId == target.Id, default)).Single();

        // Act & Assert
        await Should.ThrowAsync<NotFoundException>(() =>
            Mediator.Send(new GetAccountTransactionQuery(target.Id, transaction.Id)));
    }

    private async Task<Account> CreateAccountAsync(Guid ownerId, Guid? ledgerId = null)
    {
        if (ledgerId == null)
        {
            var ledger = await EntityStore.CreateAsync(new Ledger { OwnerId = ownerId, Name = $"Ledger {Guid.NewGuid()}" });
            await EntityStore.SaveChangesAsync();
            ledgerId = ledger.Id;
        }
        var account = await EntityStore.CreateAsync(new Account { LedgerId = ledgerId.Value, Name = $"Cash {Guid.NewGuid()}" });
        await EntityStore.SaveChangesAsync();
        return account;
    }
}
