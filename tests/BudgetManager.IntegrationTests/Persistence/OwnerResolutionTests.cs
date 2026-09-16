using BudgetManager.Domain.Entities;
using BudgetManager.Infrastructure.Persistence.Readers;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Persistence;

public class OwnerResolutionTests(ITestOutputHelper testOutputHelper, PersistenceFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    private record Seed(Guid OwnerId, Guid LedgerId, Guid AccountTransactionId);

    [Fact]
    public async Task ReadOwnerIdAsync_WhenOwnerIsOnTheEntity_ShouldReturnIt()
    {
        // Arrange
        var (seed, reader) = await SeedAsync();

        // Act
        var ownerId = await reader.ReadOwnerIdAsync<Ledger>(seed.LedgerId, x => x.OwnerId);

        // Assert
        ownerId.ShouldBe(seed.OwnerId);
    }

    [Fact]
    public async Task ReadOwnerIdAsync_WhenOwnerIsOnTheParent_ShouldReturnIt()
    {
        // Arrange
        var (seed, reader) = await SeedAsync();

        // Act
        var ownerId = await reader.ReadOwnerIdAsync<AccountTransaction>(seed.AccountTransactionId, x => x.Account.Ledger.OwnerId);

        // Assert
        ownerId.ShouldBe(seed.OwnerId);
    }

    [Fact]
    public async Task ReadOwnerIdAsync_WhenEntityDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var (_, reader) = await SeedAsync();

        // Act
        var ownerId = await reader.ReadOwnerIdAsync<Ledger>(Guid.NewGuid(), x => x.OwnerId);

        // Assert
        ownerId.ShouldBeNull();
    }

    private async Task<(Seed Seed, ResourceOwnerReader Reader)> SeedAsync()
    {
        var dbContext = GetContext();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = $"test@email{Guid.NewGuid()}",
            HashedPassword = "Test Hashed Password"
        };
        var ledger = new Ledger { Id = Guid.NewGuid(), OwnerId = user.Id, Name = "Test Ledger" };
        var account = new Account { Id = Guid.NewGuid(), LedgerId = ledger.Id, Name = "Test Account" };
        var accountTransaction = new AccountTransaction { Id = Guid.NewGuid(), AccountId = account.Id, Value = new(1, "PLN") };

        dbContext.AddRange(user, ledger, account, accountTransaction);
        await dbContext.SaveChangesAsync();

        return (new Seed(user.Id, ledger.Id, accountTransaction.Id), new ResourceOwnerReader(dbContext));
    }
}
