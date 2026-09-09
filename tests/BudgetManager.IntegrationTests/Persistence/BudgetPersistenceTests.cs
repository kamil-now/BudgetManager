using BudgetManager.Common.Enums;
using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Persistence;

public class BudgetPersistenceTests(ITestOutputHelper testOutputHelper, PersistenceFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task Delete_WhenBudgetHasFunds_DeletesFunds()
    {
        // Arrange
        var user = new User
        {
            Name = "Test User",
            Email = $"test@email{Guid.NewGuid()}",
            HashedPassword = "Test Hashed Password"
        };
        var ledger = new Ledger { OwnerId = user.Id, Name = $"Ledger {Guid.NewGuid()}" };
        var budget = new Budget { OwnerId = user.Id, LedgerId = ledger.Id, Name = $"Budget {Guid.NewGuid()}" };
        var fund = new Fund
        {
            BudgetId = budget.Id,
            Name = $"Fund {Guid.NewGuid()}",
            AllocationTemplateSequence = 0,
            AllocationTemplateValue = 100,
            AllocationTemplateType = AllocationType.Fixed
        };
        var dbContext = GetContext();
        dbContext.Users.Add(user);
        dbContext.Ledgers.Add(ledger);
        dbContext.Budgets.Add(budget);
        dbContext.Funds.Add(fund);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        // Act
        var other = GetContext();
        other.Budgets.Remove(await other.Budgets.SingleAsync(x => x.Id == budget.Id));
        await other.SaveChangesAsync(CancellationToken.None);

        // Assert
        var result = GetContext();
        (await result.Funds.AnyAsync(x => x.Id == fund.Id)).ShouldBeFalse();
    }
}
