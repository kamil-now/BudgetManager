using BudgetManager.Application.Queries;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Models;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Application;

public class GetAccountTests(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task GetAccount_WhenAccountBelongsToUser_ShouldReturnAccountWithBalance()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var account = await CreateAccountAsync(userId, new Money(150, "PLN"));

        // Act
        var result = await Mediator.Send(new GetAccountQuery(account.Id));

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(account.Id);
        result.LedgerId.ShouldBe(account.LedgerId);
        result.Name.ShouldBe(account.Name);
        result.Balance["PLN"].ShouldBe(150);
    }

    [Fact]
    public async Task GetAccount_WhenAccountHasTransactionsInSeveralCurrencies_ShouldReturnSummedBalance()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var account = await CreateAccountAsync(userId, new Money(150, "PLN"));
        await EntityStore.CreateAsync(AccountTransaction.Create(account.Id, new Money(50, "PLN"), DateTimeOffset.UtcNow, "Income"));
        await EntityStore.CreateAsync(AccountTransaction.Create(account.Id, new Money(-20, "PLN"), DateTimeOffset.UtcNow, "Expense"));
        await EntityStore.CreateAsync(AccountTransaction.Create(account.Id, new Money(10, "EUR"), DateTimeOffset.UtcNow, "Euro"));
        await EntityStore.SaveChangesAsync();

        // Act
        var result = await Mediator.Send(new GetAccountQuery(account.Id));

        // Assert
        result.Balance.Count.ShouldBe(2);
        result.Balance["PLN"].ShouldBe(180);
        result.Balance["EUR"].ShouldBe(10);
    }

    [Fact]
    public async Task GetAccount_WhenAccountBelongsToAnotherUser_ShouldThrowAuthorizationException()
    {
        // Arrange
        var otherUserId = await MockAuthenticatedUserAsync();
        var account = await CreateAccountAsync(otherUserId, new Money(10, "PLN"));

        await MockAuthenticatedUserAsync();

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() => Mediator.Send(new GetAccountQuery(account.Id)));
    }

    private async Task<Account> CreateAccountAsync(Guid ownerId, Money initialBalance)
    {
        var ledger = await EntityStore.CreateAsync(new Ledger { OwnerId = ownerId, Name = $"Ledger {Guid.NewGuid()}" });
        await EntityStore.SaveChangesAsync();

        var accountId = Guid.NewGuid();
        var account = await EntityStore.CreateAsync(new Account
        {
            Id = accountId,
            LedgerId = ledger.Id,
            Name = "Cash",
            Transactions = [AccountTransaction.CreateInitialBalance(accountId, initialBalance)]
        });
        await EntityStore.SaveChangesAsync();
        return account;
    }
}
