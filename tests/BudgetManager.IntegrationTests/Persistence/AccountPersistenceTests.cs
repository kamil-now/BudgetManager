using BudgetManager.Domain.Models;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Persistence;

public class AccountPersistenceTests(ITestOutputHelper testOutputHelper, PersistenceFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task Add_WhenLedgerDoesNotExist_ThrowsException()
    {
        // Arrange
        var user = NewUser();

        var account = new Account
        {
            Name = "Test Account",
            Description = "Test Account Description",
            LedgerId = Guid.NewGuid()
        };
        var dbContext = GetContext();
        using var transaction = await dbContext.Database.BeginTransactionAsync();

        // Act & Assert
        await Should.ThrowAsync<DbUpdateException>(() =>
        {
            dbContext.Users.Add(user);
            dbContext.Accounts.Add(account);
            return dbContext.SaveChangesAsync(CancellationToken.None);
        });
    }

    [Fact]
    public async Task Add_SavesAccount()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;

        var user = NewUser();
        var ledger = NewLedger(user);

        var account = new Account
        {
            Name = "Test Account",
            Description = "Test Account Description",
            LedgerId = ledger.Id
        };

        var dbContext = GetContext();
        using var transaction = await dbContext.Database.BeginTransactionAsync();

        // Act
        dbContext.Users.Add(user);
        dbContext.Ledgers.Add(ledger);
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        // Assert
        var result = dbContext.Accounts.FirstOrDefault(a => a.Id == account.Id);
        AssertAccount(result, account, timestamp);
    }

    [Fact]
    public async Task Add_WhenLedgerAlreadyHasAccountWithTheSameName_ThrowsException()
    {
        // Arrange
        var user = NewUser();
        var ledger = NewLedger(user);
        var name = $"Test Account {Guid.NewGuid()}";
        var dbContext = GetContext();
        dbContext.Users.Add(user);
        dbContext.Ledgers.Add(ledger);
        dbContext.Accounts.Add(new Account { LedgerId = ledger.Id, Name = name });
        await dbContext.SaveChangesAsync(CancellationToken.None);

        // Act & Assert
        var other = GetContext();
        other.Accounts.Add(new Account { LedgerId = ledger.Id, Name = name });
        await Should.ThrowAsync<DbUpdateException>(() => other.SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Add_WhenAnotherLedgerHasAccountWithTheSameName_SavesAccount()
    {
        // Arrange
        var user = NewUser();
        var ledger = NewLedger(user);
        var otherLedger = NewLedger(user);
        var name = $"Test Account {Guid.NewGuid()}";
        var dbContext = GetContext();
        dbContext.Users.Add(user);
        dbContext.Ledgers.AddRange(ledger, otherLedger);
        dbContext.Accounts.Add(new Account { LedgerId = ledger.Id, Name = name });
        await dbContext.SaveChangesAsync(CancellationToken.None);

        // Act
        var account = new Account { LedgerId = otherLedger.Id, Name = name };
        var other = GetContext();
        other.Accounts.Add(account);
        await other.SaveChangesAsync(CancellationToken.None);

        // Assert
        var result = GetContext();
        (await result.Accounts.AnyAsync(x => x.Id == account.Id)).ShouldBeTrue();
    }

    [Fact]
    public async Task Delete_WhenAccountHasTransactions_DeletesTransactions()
    {
        // Arrange
        var user = NewUser();
        var ledger = NewLedger(user);
        var account = new Account { LedgerId = ledger.Id, Name = $"Test Account {Guid.NewGuid()}" };
        var transaction = new AccountTransaction
        {
            AccountId = account.Id,
            Value = new Money(100, "EUR"),
            Date = DateTimeOffset.UtcNow
        };
        var dbContext = GetContext();
        dbContext.Users.Add(user);
        dbContext.Ledgers.Add(ledger);
        dbContext.Accounts.Add(account);
        dbContext.AccountTransactions.Add(transaction);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        // Act
        var other = GetContext();
        other.Accounts.Remove(await other.Accounts.SingleAsync(x => x.Id == account.Id));
        await other.SaveChangesAsync(CancellationToken.None);

        // Assert
        var result = GetContext();
        (await result.AccountTransactions.AnyAsync(x => x.Id == transaction.Id)).ShouldBeFalse();
    }

    [Fact]
    public async Task Add_WhenTransactionAmountExceedsPrecision_ThrowsException()
    {
        // Arrange
        var user = NewUser();
        var ledger = NewLedger(user);
        var account = new Account { LedgerId = ledger.Id, Name = $"Test Account {Guid.NewGuid()}" };
        var dbContext = GetContext();
        dbContext.Users.Add(user);
        dbContext.Ledgers.Add(ledger);
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var tooLargeAmount = (decimal)Math.Pow(10, Constants.MoneyPrecision - Constants.MoneyDecimalPlaces);

        // Act & Assert
        var other = GetContext();
        other.AccountTransactions.Add(new AccountTransaction
        {
            AccountId = account.Id,
            Value = new Money(tooLargeAmount, "EUR"),
            Date = DateTimeOffset.UtcNow
        });
        await Should.ThrowAsync<DbUpdateException>(() => other.SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public void SaveChanges_WhenAccountIsModified_StampsUpdatedAt()
    {
        // Arrange
        var user = NewUser();
        var ledger = NewLedger(user);
        var account = new Account { LedgerId = ledger.Id, Name = $"Test Account {Guid.NewGuid()}" };
        var dbContext = GetContext();
        dbContext.Users.Add(user);
        dbContext.Ledgers.Add(ledger);
        dbContext.Accounts.Add(account);
        dbContext.SaveChanges();

        // Act
        account.Description = "Updated";
        dbContext.SaveChanges();

        // Assert
        var result = GetContext().Accounts.Single(x => x.Id == account.Id);
        result.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenAccountIsModified_StampsUpdatedAt()
    {
        // Arrange
        var user = NewUser();
        var ledger = NewLedger(user);
        var account = new Account { LedgerId = ledger.Id, Name = $"Test Account {Guid.NewGuid()}" };
        var dbContext = GetContext();
        dbContext.Users.Add(user);
        dbContext.Ledgers.Add(ledger);
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        // Act
        account.Description = "Updated";
        await dbContext.SaveChangesAsync(CancellationToken.None);

        // Assert
        var result = GetContext().Accounts.Single(x => x.Id == account.Id);
        result.UpdatedAt.ShouldNotBeNull();
    }

    private static User NewUser() => new()
    {
        Name = "Test User",
        Email = $"test@email{Guid.NewGuid()}",
        HashedPassword = "Test Hashed Password"
    };

    private static Ledger NewLedger(User user) => new()
    {
        OwnerId = user.Id,
        Name = $"Test Ledger {Guid.NewGuid()}"
    };

    private static void AssertAccount(Account? result, Account expected, DateTimeOffset timestamp)
    {
        result.ShouldNotBeNull();
        result.Id.ShouldNotBe(Guid.Empty);
        result.Name.ShouldBe(expected.Name);
        result.Description.ShouldBe(expected.Description);
        result.LedgerId.ShouldBe(expected.LedgerId);
        result.CreatedAt.ShouldBeGreaterThanOrEqualTo(timestamp);
        result.UpdatedAt.ShouldBeNull();
    }
}
