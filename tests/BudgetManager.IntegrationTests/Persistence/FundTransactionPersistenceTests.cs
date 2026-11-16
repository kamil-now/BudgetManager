using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Persistence;

public class FundTransactionPersistenceTests(ITestOutputHelper testOutputHelper, PersistenceFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task Delete_WhenAccountTransactionHasAllocations_DeletesAllocations()
    {
        // Arrange
        var seed = await SeedAsync();

        // Act
        var other = GetContext();
        other.AccountTransactions.Remove(await other.AccountTransactions.SingleAsync(x => x.Id == seed.Income.Id));
        await other.SaveChangesAsync(CancellationToken.None);

        // Assert
        var result = GetContext();
        (await result.FundTransactions.AnyAsync(x => x.Id == seed.IncomeAllocation.Id)).ShouldBeFalse();
        (await result.FundTransactions.AnyAsync(x => x.Id == seed.Allocation.Transactions.Single().Id)).ShouldBeTrue();
    }

    [Fact]
    public async Task Delete_WhenAccountTransactionIsPaidFromFund_DeletesPayment()
    {
        // Arrange
        var seed = await SeedAsync();

        // Act
        var other = GetContext();
        other.AccountTransactions.Remove(await other.AccountTransactions.SingleAsync(x => x.Id == seed.Expense.Id));
        await other.SaveChangesAsync(CancellationToken.None);

        // Assert
        (await GetContext().FundTransactions.AnyAsync(x => x.Id == seed.Payment.Id)).ShouldBeFalse();
    }

    [Fact]
    public async Task Delete_WhenAllocationHasRows_DeletesRows()
    {
        // Arrange
        var seed = await SeedAsync();

        // Act
        var other = GetContext();
        other.Allocations.Remove(await other.Allocations.SingleAsync(x => x.Id == seed.Allocation.Id));
        await other.SaveChangesAsync(CancellationToken.None);

        // Assert
        var result = GetContext();
        (await result.FundTransactions.AnyAsync(x => x.AllocationId == seed.Allocation.Id)).ShouldBeFalse();
        (await result.FundTransactions.AnyAsync(x => x.Id == seed.IncomeAllocation.Id)).ShouldBeTrue();
    }

    [Fact]
    public async Task Add_WhenAllocatedFromAccountTransaction_SavesLinkToIt()
    {
        // Arrange & Act
        var seed = await SeedAsync();

        // Assert
        var result = GetContext();
        (await result.FundTransactions.SingleAsync(x => x.Id == seed.IncomeAllocation.Id)).AccountTransactionId.ShouldBe(seed.Income.Id);
    }

    [Fact]
    public async Task Add_WhenPaidFromFund_SavesNegativeRowLinkedToExpense()
    {
        // Arrange & Act
        var seed = await SeedAsync();

        // Assert
        var saved = await GetContext().FundTransactions.SingleAsync(x => x.Id == seed.Payment.Id);
        saved.ShouldSatisfyAllConditions(
            x => x.AccountTransactionId.ShouldBe(seed.Expense.Id),
            x => x.AllocationId.ShouldBeNull(),
            x => x.Value.ShouldBe(new Money(-20, "EUR")));
    }

    [Fact]
    public async Task Add_WhenInAllocation_SavesLinkToIt()
    {
        // Arrange
        var seed = await SeedAsync();
        var date = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
        var allocation = Allocation.Create(seed.Ledger.Id, "EUR", date, [new FundAmount(seed.FundId, 15)]);

        // Act
        var dbContext = GetContext();
        dbContext.Allocations.Add(allocation);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        // Assert
        var saved = await GetContext().FundTransactions.SingleAsync(x => x.AllocationId == allocation.Id);
        saved.ShouldSatisfyAllConditions(
            x => x.Value.ShouldBe(new Money(15, "EUR")),
            x => x.Date.ShouldBe(date),
            x => x.AccountTransactionId.ShouldBeNull());
    }

    [Fact]
    public async Task Add_WhenAccountTransactionDoesNotExist_ThrowsException()
    {
        // Arrange
        var seed = await SeedAsync();

        // Act & Assert
        var other = GetContext();
        other.FundTransactions.Add(new FundTransaction
        {
            FundId = seed.FundId,
            Value = new Money(1, "EUR"),
            AccountTransactionId = Guid.NewGuid()
        });
        await Should.ThrowAsync<DbUpdateException>(() => other.SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Add_WhenNeitherAccountTransactionNorAllocationIsSet_ThrowsException()
    {
        // Arrange
        var seed = await SeedAsync();

        // Act & Assert
        var other = GetContext();
        other.FundTransactions.Add(new FundTransaction { FundId = seed.FundId, Value = new Money(1, "EUR") });
        await Should.ThrowAsync<DbUpdateException>(() => other.SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Add_WhenBothAccountTransactionAndAllocationAreSet_ThrowsException()
    {
        // Arrange
        var seed = await SeedAsync();

        // Act & Assert
        var other = GetContext();
        other.FundTransactions.Add(new FundTransaction
        {
            FundId = seed.FundId,
            Value = new Money(1, "EUR"),
            AccountTransactionId = seed.Income.Id,
            AllocationId = seed.Allocation.Id
        });
        await Should.ThrowAsync<DbUpdateException>(() => other.SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Add_WhenAddedThroughLedgerGraph_InsertsInitialAllocation()
    {
        // Arrange
        var user = new User
        {
            Name = "Test User",
            Email = $"test@email{Guid.NewGuid()}",
            HashedPassword = "Test Hashed Password"
        };
        var ledger = new Ledger { OwnerId = user.Id, Name = $"Ledger {Guid.NewGuid()}" };
        var budget = new Budget { LedgerId = ledger.Id, Name = "Budget" };
        budget.AddFund("Food");
        var fund = budget.Funds.Single();
        ledger.Budgets.Add(budget);
        var allocation = Allocation.CreateInitial(ledger.Id, "EUR", [new FundAmount(fund.Id, 60)]);

        // Act
        var dbContext = GetContext();
        dbContext.AddRange(user, ledger, allocation);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        // Assert
        var result = GetContext();
        (await result.FundTransactions.SingleAsync(x => x.FundId == fund.Id)).Value.ShouldBe(new Money(60, "EUR"));
        (await result.Allocations.SingleAsync(x => x.Id == allocation.Id)).IsInitial.ShouldBeTrue();
    }

    private sealed record Seed(
        Ledger Ledger,
        Guid FundId,
        AccountTransaction Income,
        FundTransaction IncomeAllocation,
        AccountTransaction Expense,
        FundTransaction Payment,
        Allocation Allocation);

    private async Task<Seed> SeedAsync()
    {
        var user = new User
        {
            Name = "Test User",
            Email = $"test@email{Guid.NewGuid()}",
            HashedPassword = "Test Hashed Password"
        };
        var ledger = new Ledger { OwnerId = user.Id, Name = $"Ledger {Guid.NewGuid()}" };
        var account = new Account { LedgerId = ledger.Id, Name = $"Account {Guid.NewGuid()}" };
        var budget = new Budget { LedgerId = ledger.Id, Name = $"Budget {Guid.NewGuid()}" };
        var fund = new Fund { BudgetId = budget.Id, Name = $"Fund {Guid.NewGuid()}" };
        var income = AccountTransaction.Create(account.Id, new Money(100, "EUR"), DateTimeOffset.UtcNow, "Salary");
        var incomeAllocation = FundTransaction.Allocate(fund.Id, new Money(40, "EUR"), income);
        var expense = AccountTransaction.Create(account.Id, new Money(-20, "EUR"), DateTimeOffset.UtcNow, "Food");
        var payment = FundTransaction.Pay(fund.Id, expense);
        var allocation = Allocation.Create(ledger.Id, "EUR", DateTimeOffset.UtcNow, [new FundAmount(fund.Id, 10)]);

        var dbContext = GetContext();
        dbContext.AddRange(user, ledger, account, budget, fund, income, incomeAllocation, expense, payment, allocation);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        return new Seed(ledger, fund.Id, income, incomeAllocation, expense, payment, allocation);
    }
}
