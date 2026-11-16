using BudgetManager.Application.Commands;
using BudgetManager.Application.Models;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Models;
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

        var unchanged = await EntityStore.GetAsync<AccountTransaction>(transaction.Id);
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
        var updated = await EntityStore.GetAsync<AccountTransaction>(transaction.Id);
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
        var command = new UpdateAccountTransactionCommand(transaction.Id, account.Id, new(50, "PLN"), date, "Updated Title");

        // Act
        await Mediator.Send(command);

        // Assert
        var updated = await EntityStore.GetAsync<AccountTransaction>(transaction.Id);
        updated.AccountId.ShouldBe(account.Id);
        updated.Title.ShouldBe(command.Title);
        updated.Value.ShouldBe(command.Amount);
        updated.Date.ShouldBe(command.Date);
    }

    [Fact]
    public async Task UpdateAccountTransaction_WhenAllocationIsSent_ShouldReplaceAllocations()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");
        var accountId = ledger.Accounts.Single().Id;
        var id = await CreateAllocatedIncomeAsync(accountId, 100, FundId(ledger, "Food"), 100);

        // Act
        await Mediator.Send(new UpdateAccountTransactionCommand(id, accountId, new(200, "EUR"), DateTimeOffset.UtcNow, "Salary",
            Allocation: new AllocationInput(Lines: [new(FundId(ledger, "Rent"), 0, AllocationType.Fixed, 150)])));

        // Assert
        (await AllocationsOfAsync(id)).ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            x => x.FundId.ShouldBe(FundId(ledger, "Rent")),
            x => x.Value.ShouldBe(new Money(150, "EUR")));
    }

    [Fact]
    public async Task UpdateAccountTransaction_WhenAllocationLinesAreEmpty_ShouldClearAllocations()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var accountId = ledger.Accounts.Single().Id;
        var id = await CreateAllocatedIncomeAsync(accountId, 100, FundId(ledger, "Food"), 100);

        // Act
        await Mediator.Send(new UpdateAccountTransactionCommand(id, accountId, new(-30, "EUR"), DateTimeOffset.UtcNow, "Refund",
            Allocation: new AllocationInput(Lines: [])));

        // Assert
        (await AllocationsOfAsync(id)).ShouldBeEmpty();
    }

    [Fact]
    public async Task UpdateAccountTransaction_WhenAllocationIsOmittedAndAmountFallsBelowAllocations_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var accountId = ledger.Accounts.Single().Id;
        var id = await CreateAllocatedIncomeAsync(accountId, 100, FundId(ledger, "Food"), 80);
        var command = new UpdateAccountTransactionCommand(id, accountId, new(79.99m, "EUR"), DateTimeOffset.UtcNow, "Salary");

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe("Transaction has fund allocations; send a new allocation.");
    }

    [Fact]
    public async Task UpdateAccountTransaction_WhenAllocationIsOmittedAndCurrencyChanges_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var accountId = ledger.Accounts.Single().Id;
        var id = await CreateAllocatedIncomeAsync(accountId, 100, FundId(ledger, "Food"), 80);
        var command = new UpdateAccountTransactionCommand(id, accountId, new(100, "USD"), DateTimeOffset.UtcNow, "Salary");

        // Act & Assert
        await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
    }

    [Fact]
    public async Task UpdateAccountTransaction_WhenAllocationIsOmitted_ShouldKeepAllocationsAndSyncTheirDateAndTitle()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var accountId = ledger.Accounts.Single().Id;
        var id = await CreateAllocatedIncomeAsync(accountId, 100, FundId(ledger, "Food"), 80);
        var date = new DateTimeOffset(2026, 3, 15, 0, 0, 0, TimeSpan.Zero);

        // Act
        await Mediator.Send(new UpdateAccountTransactionCommand(id, accountId, new(90, "EUR"), date, "Bonus"));

        // Assert
        (await AllocationsOfAsync(id)).ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            x => x.Value.ShouldBe(new Money(80, "EUR")),
            x => x.Date.ShouldBe(date),
            x => x.Title.ShouldBe("Bonus"));
    }

    [Fact]
    public async Task UpdateAccountTransaction_WhenInitialBalanceIsAllocated_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var accountId = ledger.Accounts.Single().Id;
        var initialBalance = (await EntityStore.GetAsync<AccountTransaction>(x => x.AccountId == accountId, default)).Single();
        var command = new UpdateAccountTransactionCommand(initialBalance.Id, accountId, new(100, "EUR"), default, "Initial balance",
            Allocation: new AllocationInput(Lines: [new(FundId(ledger, "Food"), 0, AllocationType.Fixed, 50)]));

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe("Initial balance cannot be allocated to funds.");
    }

    [Fact]
    public async Task UpdateAccountTransaction_WhenInitialBalanceDateChanges_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var accountId = ledger.Accounts.Single().Id;
        var initialBalance = (await EntityStore.GetAsync<AccountTransaction>(x => x.AccountId == accountId, default)).Single();
        var command = new UpdateAccountTransactionCommand(initialBalance.Id, accountId, new(100, "EUR"), DateTimeOffset.UtcNow, "Initial balance");

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe("Initial balance date cannot be changed.");
    }

    [Fact]
    public async Task UpdateAccountTransaction_WhenInitialBalanceAmountChanges_ShouldUpdateIt()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var accountId = ledger.Accounts.Single().Id;
        var initialBalance = (await EntityStore.GetAsync<AccountTransaction>(x => x.AccountId == accountId, default)).Single();

        // Act
        await Mediator.Send(new UpdateAccountTransactionCommand(initialBalance.Id, accountId, new(70, "EUR"), initialBalance.Date, "Initial balance"));

        // Assert
        (await EntityStore.GetAsync<AccountTransaction>(initialBalance.Id)).Value.ShouldBe(new Money(70, "EUR"));
    }

    [Fact]
    public async Task UpdateAccountTransaction_WhenFundIsSetOnInitialBalance_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var accountId = ledger.Accounts.Single().Id;
        var initialBalance = (await EntityStore.GetAsync<AccountTransaction>(x => x.AccountId == accountId, default)).Single();
        var command = new UpdateAccountTransactionCommand(initialBalance.Id, accountId, new(-10, "EUR"), initialBalance.Date, "Initial balance", FundId: FundId(ledger, "Food"));

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe("Initial balance cannot be paid from a fund.");
    }

    [Fact]
    public async Task UpdateAccountTransaction_WhenFundIsChanged_ShouldMovePaymentToTheNewFund()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");
        var accountId = ledger.Accounts.Single().Id;
        var id = await CreateExpenseAsync(accountId, 40, FundId(ledger, "Food"));

        // Act
        await Mediator.Send(new UpdateAccountTransactionCommand(id, accountId, new(-40, "EUR"), DateTimeOffset.UtcNow, "Groceries", FundId: FundId(ledger, "Rent")));

        // Assert
        (await AllocationsOfAsync(id)).ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            x => x.FundId.ShouldBe(FundId(ledger, "Rent")),
            x => x.Value.ShouldBe(new Money(-40, "EUR")));
        (await FundBalanceAsync(ledger.Id, FundId(ledger, "Food"))).ShouldBe(0);
    }

    [Fact]
    public async Task UpdateAccountTransaction_WhenFundIsRemoved_ShouldDeletePayment()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var accountId = ledger.Accounts.Single().Id;
        var id = await CreateExpenseAsync(accountId, 40, FundId(ledger, "Food"));

        // Act
        await Mediator.Send(new UpdateAccountTransactionCommand(id, accountId, new(-40, "EUR"), DateTimeOffset.UtcNow, "Groceries"));

        // Assert
        (await AllocationsOfAsync(id)).ShouldBeEmpty();
    }

    [Fact]
    public async Task UpdateAccountTransaction_WhenAmountAndDateChange_ShouldMakePaymentFollowTheExpense()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var accountId = ledger.Accounts.Single().Id;
        var food = FundId(ledger, "Food");
        var id = await CreateExpenseAsync(accountId, 40, food);
        var date = new DateTimeOffset(2026, 3, 15, 0, 0, 0, TimeSpan.Zero);

        // Act
        await Mediator.Send(new UpdateAccountTransactionCommand(id, accountId, new(-55, "EUR"), date, "Bigger groceries", FundId: food));

        // Assert
        (await AllocationsOfAsync(id)).ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            x => x.Value.ShouldBe(new Money(-55, "EUR")),
            x => x.Date.ShouldBe(date),
            x => x.Title.ShouldBe("Bigger groceries"));
    }

    [Fact]
    public async Task UpdateAccountTransaction_WhenFundIsAddedToExpense_ShouldCreatePayment()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var accountId = ledger.Accounts.Single().Id;
        var id = await CreateExpenseAsync(accountId, 40, null);

        // Act
        await Mediator.Send(new UpdateAccountTransactionCommand(id, accountId, new(-40, "EUR"), DateTimeOffset.UtcNow, "Groceries", FundId: FundId(ledger, "Food")));

        // Assert
        (await FundBalanceAsync(ledger.Id, FundId(ledger, "Food"))).ShouldBe(-40);
    }

    [Fact]
    public async Task UpdateAccountTransaction_WhenExpenseBecomesIncomeWithFund_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var accountId = ledger.Accounts.Single().Id;
        var food = FundId(ledger, "Food");
        var id = await CreateExpenseAsync(accountId, 40, food);

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() =>
            Mediator.Send(new UpdateAccountTransactionCommand(id, accountId, new(40, "EUR"), DateTimeOffset.UtcNow, "Groceries", FundId: food)));
        ex.Message.ShouldBe("Only an expense can be paid from a fund.");
    }

    [Fact]
    public async Task UpdateAccountTransaction_WhenExpenseBecomesIncome_ShouldDeletePayment()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var accountId = ledger.Accounts.Single().Id;
        var id = await CreateExpenseAsync(accountId, 40, FundId(ledger, "Food"));

        // Act
        await Mediator.Send(new UpdateAccountTransactionCommand(id, accountId, new(40, "EUR"), DateTimeOffset.UtcNow, "Refund"));

        // Assert
        (await AllocationsOfAsync(id)).ShouldBeEmpty();
    }

    [Fact]
    public async Task UpdateAccountTransaction_WhenPayingFundBelongsToAnotherLedger_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var otherLedger = await SeedLedgerAsync("Rent");
        var accountId = ledger.Accounts.Single().Id;
        var id = await CreateExpenseAsync(accountId, 40, null);

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() =>
            Mediator.Send(new UpdateAccountTransactionCommand(id, accountId, new(-40, "EUR"), DateTimeOffset.UtcNow, "Groceries", FundId: FundId(otherLedger, "Rent"))));
        ex.Message.ShouldBe("Fund must belong to a budget in the account's ledger.");
    }

    private async Task<Guid> CreateExpenseAsync(Guid accountId, decimal amount, Guid? fundId)
        => await Mediator.Send(new CreateAccountTransactionCommand(accountId, new(-amount, "EUR"), DateTimeOffset.UtcNow, "Groceries", FundId: fundId));

    private async Task<Guid> CreateAllocatedIncomeAsync(Guid accountId, decimal amount, Guid fundId, decimal allocated)
        => await Mediator.Send(new CreateAccountTransactionCommand(accountId, new(amount, "EUR"), DateTimeOffset.UtcNow, "Salary",
            Allocation: new AllocationInput(Lines: [new(fundId, 0, AllocationType.Fixed, allocated)])));

    private async Task<FundTransaction[]> AllocationsOfAsync(Guid accountTransactionId)
        => [.. await EntityStore.GetAsync<FundTransaction>(x => x.AccountTransactionId == accountTransactionId, default)];

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

    private async Task<AccountTransaction> CreateTransactionAsync(Guid accountId)
    {
        var transaction = await EntityStore.CreateAsync(AccountTransaction.Create(accountId, new(100, "PLN"), DateTimeOffset.UtcNow, "Test Transaction"));
        await EntityStore.SaveChangesAsync();
        return transaction;
    }
}
