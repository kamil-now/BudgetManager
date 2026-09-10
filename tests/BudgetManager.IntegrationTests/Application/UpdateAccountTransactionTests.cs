using BudgetManager.Application.Commands;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Entities;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Application;

public class UpdateAccountTransactionTests(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task UpdateAccountTransaction_WhenTargetAccountBelongsToAnotherLedger_ShouldThrowException()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var account = await CreateAccountAsync(userId);
        var otherLedgerAccount = await CreateAccountAsync(userId);
        var transaction = await CreateTransactionAsync(account.Id);

        var command = new UpdateAccountTransactionCommand(transaction.Id, otherLedgerAccount.Id, new(50, "PLN"), DateTimeOffset.UtcNow, "Updated Title");

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe("Transaction target account must belong to the same ledger as the current account.");

        var unchanged = await BudgetManagerService.GetAsync<AccountTransaction>(transaction.Id);
        unchanged.AccountId.ShouldBe(account.Id);
    }

    [Fact]
    public async Task UpdateAccountTransaction_WhenTargetAccountBelongsToTheSameLedger_ShouldMoveTransaction()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var account = await CreateAccountAsync(userId);
        var sameLedgerAccount = await CreateAccountAsync(userId, account.LedgerId);
        var transaction = await CreateTransactionAsync(account.Id);

        var command = new UpdateAccountTransactionCommand(transaction.Id, sameLedgerAccount.Id, new(50, "PLN"), DateTimeOffset.UtcNow, "Updated Title");

        // Act
        await Mediator.Send(command);

        // Assert
        var updated = await BudgetManagerService.GetAsync<AccountTransaction>(transaction.Id);
        updated.AccountId.ShouldBe(sameLedgerAccount.Id);
    }

    [Fact]
    public async Task UpdateAccountTransaction_WhenTargetAccountBelongsToAnotherUser_ShouldThrowException()
    {
        // Arrange
        var otherUserId = await MockAuthenticatedUserAsync();
        var otherUserAccount = await CreateAccountAsync(otherUserId);

        var userId = await MockAuthenticatedUserAsync();
        var account = await CreateAccountAsync(userId);
        var transaction = await CreateTransactionAsync(account.Id);

        var command = new UpdateAccountTransactionCommand(transaction.Id, otherUserAccount.Id, new(50, "PLN"), DateTimeOffset.UtcNow, "Updated Title");

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() => Mediator.Send(command));
    }

    [Fact]
    public async Task UpdateAccountTransaction_ShouldUpdateEntity()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var account = await CreateAccountAsync(userId);
        var transaction = await CreateTransactionAsync(account.Id);

        var date = DateTimeOffset.UtcNow;
        var command = new UpdateAccountTransactionCommand(transaction.Id, account.Id, new(50, "PLN"), date, "Updated Title", "Updated Comment", ["tag"]);

        // Act
        await Mediator.Send(command);

        // Assert
        var updated = await BudgetManagerService.GetAsync<AccountTransaction>(transaction.Id);
        updated.AccountId.ShouldBe(account.Id);
        updated.Title.ShouldBe(command.Title);
        updated.Comment.ShouldBe(command.Comment);
        updated.Value.ShouldBe(command.Amount);
        updated.Date.ShouldBe(command.Date);
        updated.Tags.ShouldBe(["tag"]);
    }

    private async Task<Account> CreateAccountAsync(Guid ownerId, Guid? ledgerId = null)
    {
        if (ledgerId == null)
        {
            var ledger = await BudgetManagerService.CreateAsync(new Ledger { OwnerId = ownerId, Name = $"Ledger {Guid.NewGuid()}" });
            await BudgetManagerService.SaveChangesAsync();
            ledgerId = ledger.Id;
        }

        var account = await BudgetManagerService.CreateAsync(new Account { LedgerId = ledgerId.Value, Name = $"Account {Guid.NewGuid()}" });
        await BudgetManagerService.SaveChangesAsync();
        return account;
    }

    private async Task<AccountTransaction> CreateTransactionAsync(Guid accountId)
    {
        var transaction = await BudgetManagerService.CreateAsync(new AccountTransaction
        {
            AccountId = accountId,
            Value = new(100, "PLN"),
            Date = DateTimeOffset.UtcNow,
            Title = "Test Transaction"
        });
        await BudgetManagerService.SaveChangesAsync();
        return transaction;
    }
}
