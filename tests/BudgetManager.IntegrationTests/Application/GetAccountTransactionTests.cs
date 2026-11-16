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
            AccountTransaction.Create(account.Id, new Money(50, "PLN"), DateTimeOffset.UtcNow, "Groceries"));
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
    }

    [Fact]
    public async Task GetAccountTransaction_WhenExpenseIsPaidFromFund_ShouldReturnFundId()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var accountId = ledger.Accounts.Single().Id;
        var id = await Mediator.Send(new CreateAccountTransactionCommand(
            accountId, new(-30, "EUR"), DateTimeOffset.UtcNow, "Groceries", FundId: FundId(ledger, "Food")));

        // Act
        var result = await Mediator.Send(new GetAccountTransactionQuery(accountId, id));

        // Assert
        result.ShouldSatisfyAllConditions(
            x => x.FundId.ShouldBe(FundId(ledger, "Food")),
            x => x.Allocations.ShouldBeEmpty());
    }

    [Fact]
    public async Task GetAccountTransaction_WhenExpenseIsNotPaidFromFund_ShouldHaveNoFundId()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var accountId = ledger.Accounts.Single().Id;
        var id = await Mediator.Send(new CreateAccountTransactionCommand(accountId, new(-30, "EUR"), DateTimeOffset.UtcNow, "Groceries"));

        // Act
        var result = await Mediator.Send(new GetAccountTransactionQuery(accountId, id));

        // Assert
        result.FundId.ShouldBeNull();
    }

    [Fact]
    public async Task GetAccountTransaction_WhenIncomeIsAllocated_ShouldReturnAllocationsAndNoFundId()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var accountId = ledger.Accounts.Single().Id;
        var id = await Mediator.Send(new CreateAccountTransactionCommand(
            accountId, new(100, "EUR"), DateTimeOffset.UtcNow, "Salary",
            Allocation: new BudgetManager.Application.Models.AllocationInput(Lines: [new(FundId(ledger, "Food"), 0, BudgetManager.Domain.Enums.AllocationType.Fixed, 60)])));

        // Act
        var result = await Mediator.Send(new GetAccountTransactionQuery(accountId, id));

        // Assert
        result.ShouldSatisfyAllConditions(
            x => x.FundId.ShouldBeNull(),
            x => x.Allocations.ShouldHaveSingleItem().Amount.ShouldBe(60));
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
        var transaction = (await EntityStore.GetAsync<AccountTransaction>(x => x.AccountId == source.Id, default)).Single();

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
        var transaction = (await EntityStore.GetAsync<AccountTransaction>(x => x.AccountId == target.Id, default)).Single();

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
