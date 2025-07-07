using BudgetManager.Domain.Entities;
using BudgetManager.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Persistence;

public class AccountPersistenceTests(ITestOutputHelper testOutputHelper, PersistenceFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task Add_WhenUserDoesNotExist_ThrowsException()
    {
        // Arrange
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Name = "Test Account",
            UserId = Guid.NewGuid()
        };
        var dbContext = GetContext();
        using var transaction = await dbContext.Database.BeginTransactionAsync();

        // Act & Assert
        await Should.ThrowAsync<DbUpdateException>(() =>
        {
            dbContext.Accounts.Add(account);
            return dbContext.SaveChangesAsync(CancellationToken.None);
        });
    }

    [Fact]
    public async Task Add_WhenLedgerDoesNotExist_ThrowsException()
    {
        // Arrange
        var user = new User()
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = $"test@email{Guid.NewGuid()}",
            HashedPassword = "Test Hashed Password",
        };

        var account = new Account
        {
            Name = "Test Account",
            Description = "Test Account Description",
            UserId = user.Id,
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

        var user = new User()
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = $"test@email{Guid.NewGuid()}",
            HashedPassword = "Test Hashed Password",
        };

        var ledger = new Ledger()
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = "Test Ledger"
        };

        var account = new Account
        {
            Name = "Test Account",
            Description = "Test Account Description",
            UserId = user.Id,
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
    public async Task Add_WhenLedgerIdIsNull_SavesAccount()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;

        var user = new User()
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = $"test@email{Guid.NewGuid()}",
            HashedPassword = "Test Hashed Password",
        };

        var account = new Account
        {
            Name = "Test Account",
            Description = "Test Account Description",
            UserId = user.Id,
            LedgerId = null,
        };

        var dbContext = GetContext();
        using var transaction = await dbContext.Database.BeginTransactionAsync();

        // Act
        dbContext.Users.Add(user);
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync(CancellationToken.None);


        // Assert
        var result = dbContext.Accounts.FirstOrDefault(a => a.Id == account.Id);
        AssertAccount(result, account, timestamp);
    }

    private static void AssertAccount(Account? result, Account expected, DateTimeOffset timestamp)
    {
        result.ShouldNotBeNull();
        result.Id.ShouldNotBe(Guid.Empty);
        result.Name.ShouldBe(expected.Name);
        result.Description.ShouldBe(expected.Description);
        result.UserId.ShouldBe(expected.UserId);
        result.LedgerId.ShouldBe(expected.LedgerId);
        result.CreatedAt.ShouldBeGreaterThanOrEqualTo(timestamp);
        result.UpdatedAt.ShouldBeNull();
    }
}
