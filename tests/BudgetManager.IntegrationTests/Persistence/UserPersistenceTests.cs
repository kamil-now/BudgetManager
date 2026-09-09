using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Persistence;

public class UserPersistenceTests(ITestOutputHelper testOutputHelper, PersistenceFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task Add_WhenEmailIsDuplicated_ThrowsException()
    {
        // Arrange
        var email = $"test@email{Guid.NewGuid()}";
        var dbContext = GetContext();
        dbContext.Users.Add(NewUser(email));
        await dbContext.SaveChangesAsync(CancellationToken.None);

        // Act & Assert
        var other = GetContext();
        other.Users.Add(NewUser(email));
        await Should.ThrowAsync<DbUpdateException>(() => other.SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Add_WhenHashedPasswordIsTooLong_ThrowsException()
    {
        // Arrange
        var user = NewUser($"test@email{Guid.NewGuid()}");
        user.HashedPassword = new string('a', Constants.HashedPasswordLength + 1);
        var dbContext = GetContext();

        // Act & Assert
        dbContext.Users.Add(user);
        await Should.ThrowAsync<DbUpdateException>(() => dbContext.SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Delete_WhenUserHasLedgers_ThrowsException()
    {
        // Arrange
        var user = NewUser($"test@email{Guid.NewGuid()}");
        var ledger = new Ledger { OwnerId = user.Id, Name = $"Ledger {Guid.NewGuid()}" };
        var dbContext = GetContext();
        dbContext.Users.Add(user);
        dbContext.Ledgers.Add(ledger);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        // Act & Assert
        var other = GetContext();
        other.Users.Remove(await other.Users.SingleAsync(x => x.Id == user.Id));
        await Should.ThrowAsync<DbUpdateException>(() => other.SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Delete_WhenUserHasAccounts_DeletesAccounts()
    {
        // Arrange
        var user = NewUser($"test@email{Guid.NewGuid()}");
        var account = new Account { OwnerId = user.Id, Name = $"Account {Guid.NewGuid()}" };
        var dbContext = GetContext();
        dbContext.Users.Add(user);
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        // Act
        var other = GetContext();
        other.Users.Remove(await other.Users.SingleAsync(x => x.Id == user.Id));
        await other.SaveChangesAsync(CancellationToken.None);

        // Assert
        var result = GetContext();
        (await result.Accounts.AnyAsync(x => x.Id == account.Id)).ShouldBeFalse();
    }

    private static User NewUser(string email) => new()
    {
        Name = "Test User",
        Email = email,
        HashedPassword = "Test Hashed Password"
    };
}
