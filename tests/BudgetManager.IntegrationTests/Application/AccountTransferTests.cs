using BudgetManager.Application.Commands;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Models;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Application;

public class AccountTransferTests(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task CreateAccountTransfer_ShouldCreateOppositeTransactions()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var source = await CreateAccountAsync(userId);
        var target = await CreateAccountAsync(userId);

        // Act
        var id = await Mediator.Send(new CreateAccountTransferCommand(source.Id, target.Id, new(100, "PLN"), DateTimeOffset.UtcNow, "title", null, ["tag"]));

        // Assert
        var (expense, income) = await GetTransactionsAsync(id);
        expense.Value.ShouldBe(new Money(-100, "PLN"));
        income.Value.ShouldBe(new Money(100, "PLN"));
        income.Tags.ShouldBe(["tag"]);
    }

    [Fact]
    public async Task CreateAccountTransfer_WhenValueIsNegative_ShouldThrowValidationException()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var source = await CreateAccountAsync(userId);
        var target = await CreateAccountAsync(userId);

        // Act & Assert
        await Should.ThrowAsync<ValidationException>(() =>
            Mediator.Send(new CreateAccountTransferCommand(source.Id, target.Id, new(-100, "PLN"), DateTimeOffset.UtcNow, null)));
    }

    [Fact]
    public async Task CreateCurrencyExchange_ShouldStoreSellAsExpense()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var account = await CreateAccountAsync(userId);

        // Act
        var id = await Mediator.Send(new CreateCurrencyExchangeCommand(account.Id, null, new(90, "EUR"), new(100, "USD"), DateTimeOffset.UtcNow, null));

        // Assert
        var (expense, income) = await GetTransactionsAsync(id);
        expense.Value.ShouldBe(new Money(-100, "USD"));
        income.Value.ShouldBe(new Money(90, "EUR"));
    }

    [Fact]
    public async Task CreateCurrencyExchange_WhenSellIsNegative_ShouldThrowValidationException()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var account = await CreateAccountAsync(userId);

        // Act & Assert
        await Should.ThrowAsync<ValidationException>(() =>
            Mediator.Send(new CreateCurrencyExchangeCommand(account.Id, null, new(90, "EUR"), new(-100, "USD"), DateTimeOffset.UtcNow, null)));
    }

    [Fact]
    public async Task UpdateAccountTransfer_ShouldUpdateBothTransactions()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var source = await CreateAccountAsync(userId);
        var target = await CreateAccountAsync(userId);
        var id = await Mediator.Send(new CreateAccountTransferCommand(source.Id, target.Id, new(100, "PLN"), DateTimeOffset.UtcNow, null));
        var date = DateTimeOffset.UtcNow.AddDays(-1);

        // Act
        await Mediator.Send(new UpdateAccountTransferCommand(id, source.Id, target.Id, new(40, "PLN"), date, "updated"));

        // Assert
        var (expense, income) = await GetTransactionsAsync(id);
        expense.ShouldSatisfyAllConditions(
            x => x.Value.ShouldBe(new Money(-40, "PLN")),
            x => x.Date.ShouldBe(date),
            x => x.Title.ShouldBe("updated"));
        income.ShouldSatisfyAllConditions(
            x => x.Value.ShouldBe(new Money(40, "PLN")),
            x => x.Date.ShouldBe(date),
            x => x.Title.ShouldBe("updated"));
    }

    [Fact]
    public async Task UpdateAccountTransfer_WhenTransferBelongsToAnotherUser_ShouldThrowAuthorizationException()
    {
        // Arrange
        var otherUserId = await MockAuthenticatedUserAsync();
        var otherSource = await CreateAccountAsync(otherUserId);
        var otherTarget = await CreateAccountAsync(otherUserId);
        var id = await Mediator.Send(new CreateAccountTransferCommand(otherSource.Id, otherTarget.Id, new(100, "PLN"), DateTimeOffset.UtcNow, null));

        var userId = await MockAuthenticatedUserAsync();
        var source = await CreateAccountAsync(userId);
        var target = await CreateAccountAsync(userId);

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() =>
            Mediator.Send(new UpdateAccountTransferCommand(id, source.Id, target.Id, new(40, "PLN"), DateTimeOffset.UtcNow, null)));
    }

    [Fact]
    public async Task UpdateCurrencyExchange_ShouldUpdateBothTransactions()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var account = await CreateAccountAsync(userId);
        var id = await Mediator.Send(new CreateCurrencyExchangeCommand(account.Id, null, new(90, "EUR"), new(100, "USD"), DateTimeOffset.UtcNow, null));

        // Act
        await Mediator.Send(new UpdateCurrencyExchangeCommand(id, account.Id, null, new(45, "EUR"), new(50, "USD"), DateTimeOffset.UtcNow, null));

        // Assert
        var (expense, income) = await GetTransactionsAsync(id);
        expense.Value.ShouldBe(new Money(-50, "USD"));
        income.Value.ShouldBe(new Money(45, "EUR"));
    }

    [Fact]
    public async Task UpdateAccountTransaction_WhenTransactionIsTransfer_ShouldThrowValidationException()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var source = await CreateAccountAsync(userId);
        var target = await CreateAccountAsync(userId);
        var id = await Mediator.Send(new CreateAccountTransferCommand(source.Id, target.Id, new(100, "PLN"), DateTimeOffset.UtcNow, null));
        var (_, income) = await GetTransactionsAsync(id);

        // Act & Assert
        await Should.ThrowAsync<ValidationException>(() =>
            Mediator.Send(new UpdateAccountTransactionCommand(income.Id, target.Id, new(500, "PLN"), DateTimeOffset.UtcNow, null)));
    }

    [Fact]
    public async Task UpdateAccountTransaction_WhenInitialBalanceMovesToAnotherAccount_ShouldThrowValidationException()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var account = await CreateAccountAsync(userId);
        var otherAccount = await CreateAccountAsync(userId, account.LedgerId);
        var initialBalance = await EntityStore.CreateAsync(AccountTransaction.CreateInitialBalance(account.Id, new(100, "PLN")));
        await EntityStore.SaveChangesAsync();

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() =>
            Mediator.Send(new UpdateAccountTransactionCommand(initialBalance.Id, otherAccount.Id, new(100, "PLN"), DateTimeOffset.UtcNow, null)));
        ex.Message.ShouldBe("Initial balance cannot be moved to another account.");
    }

    private async Task<(AccountTransaction Expense, AccountTransaction Income)> GetTransactionsAsync(Guid transferId)
    {
        var transfer = await EntityStore.GetAsync<AccountTransfer>(transferId);
        var expense = await EntityStore.GetAsync<AccountTransaction>(transfer.ExpenseId);
        var income = await EntityStore.GetAsync<AccountTransaction>(transfer.IncomeId);
        return (expense, income);
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
