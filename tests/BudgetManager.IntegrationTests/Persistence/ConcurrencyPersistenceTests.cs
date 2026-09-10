using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Persistence;

public class ConcurrencyPersistenceTests(ITestOutputHelper testOutputHelper, PersistenceFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task SaveChangesAsync_WhenAccountWasModifiedByAnotherContext_ThrowsDbUpdateConcurrencyException()
    {
        // Arrange
        var account = await AddAccountAsync();

        var first = GetContext();
        var second = GetContext();
        var firstAccount = await first.Accounts.SingleAsync(x => x.Id == account.Id);
        var secondAccount = await second.Accounts.SingleAsync(x => x.Id == account.Id);

        firstAccount.Description = "First";
        await first.SaveChangesAsync(CancellationToken.None);

        // Act
        secondAccount.Description = "Second";

        // Assert
        await Should.ThrowAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync(CancellationToken.None));

        var result = await GetContext().Accounts.SingleAsync(x => x.Id == account.Id);
        result.Description.ShouldBe("First");
    }

    [Fact]
    public async Task SaveChangesAsync_WhenAccountWasNotModifiedByAnotherContext_SavesChanges()
    {
        // Arrange
        var account = await AddAccountAsync();

        var dbContext = GetContext();
        var tracked = await dbContext.Accounts.SingleAsync(x => x.Id == account.Id);

        // Act
        tracked.Description = "Updated";
        await dbContext.SaveChangesAsync(CancellationToken.None);

        // Assert
        var result = await GetContext().Accounts.SingleAsync(x => x.Id == account.Id);
        result.Description.ShouldBe("Updated");
    }

    private async Task<Account> AddAccountAsync()
    {
        var user = new User
        {
            Name = "Test User",
            Email = $"test@email{Guid.NewGuid()}",
            HashedPassword = "Test Hashed Password"
        };
        var ledger = new Ledger
        {
            OwnerId = user.Id,
            Name = $"Test Ledger {Guid.NewGuid()}"
        };
        var account = new Account
        {
            LedgerId = ledger.Id,
            Name = $"Test Account {Guid.NewGuid()}"
        };

        var dbContext = GetContext();
        dbContext.Users.Add(user);
        dbContext.Ledgers.Add(ledger);
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        return account;
    }
}
