using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Persistence;

public class LedgerPersistenceTests(ITestOutputHelper testOutputHelper, PersistenceFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task Delete_WhenLedgerHasAccounts_ThrowsException()
    {
        // Arrange
        var user = NewUser();
        var ledger = new Ledger { OwnerId = user.Id, Name = $"Ledger {Guid.NewGuid()}" };
        var account = new Account { OwnerId = user.Id, LedgerId = ledger.Id, Name = $"Account {Guid.NewGuid()}" };
        var dbContext = GetContext();
        dbContext.Users.Add(user);
        dbContext.Ledgers.Add(ledger);
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        // Act & Assert
        var other = GetContext();
        other.Ledgers.Remove(await other.Ledgers.SingleAsync(x => x.Id == ledger.Id));
        await Should.ThrowAsync<DbUpdateException>(() => other.SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Delete_WhenLedgerHasBudgets_ThrowsException()
    {
        // Arrange
        var user = NewUser();
        var ledger = new Ledger { OwnerId = user.Id, Name = $"Ledger {Guid.NewGuid()}" };
        var budget = new Budget { OwnerId = user.Id, LedgerId = ledger.Id, Name = $"Budget {Guid.NewGuid()}" };
        var dbContext = GetContext();
        dbContext.Users.Add(user);
        dbContext.Ledgers.Add(ledger);
        dbContext.Budgets.Add(budget);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        // Act & Assert
        var other = GetContext();
        other.Ledgers.Remove(await other.Ledgers.SingleAsync(x => x.Id == ledger.Id));
        await Should.ThrowAsync<DbUpdateException>(() => other.SaveChangesAsync(CancellationToken.None));
    }

    private static User NewUser() => new()
    {
        Name = "Test User",
        Email = $"test@email{Guid.NewGuid()}",
        HashedPassword = "Test Hashed Password"
    };
}
