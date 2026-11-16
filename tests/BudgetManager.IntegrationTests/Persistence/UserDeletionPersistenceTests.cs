using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Models;
using BudgetManager.Infrastructure.Persistence;
using BudgetManager.Infrastructure.Persistence.Services;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Persistence;

public class UserDeletionPersistenceTests(ITestOutputHelper testOutputHelper, PersistenceFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    private sealed record Seed(Guid UserId, Guid LedgerId, Guid[] AccountIds, Guid[] BudgetIds, Guid[] FundIds, Guid[] TransactionIds);

    [Fact]
    public async Task DeleteUserAsync_WhenUserOwnsData_ShouldDeleteEverything()
    {
        // Arrange
        var seed = await SeedAsync();
        var store = new EntityStore(GetContext());

        // Act
        await store.RunInTransactionAsync(() => store.DeleteUserAsync(seed.UserId));

        // Assert
        var result = GetContext();
        (await result.Users.AnyAsync(x => x.Id == seed.UserId)).ShouldBeFalse();
        (await result.Ledgers.AnyAsync(x => x.Id == seed.LedgerId)).ShouldBeFalse();
        (await result.Accounts.AnyAsync(x => seed.AccountIds.Contains(x.Id))).ShouldBeFalse();
        (await result.AccountTransactions.AnyAsync(x => seed.TransactionIds.Contains(x.Id))).ShouldBeFalse();
        (await result.AccountTransfers.AnyAsync(x => seed.TransactionIds.Contains(x.IncomeId))).ShouldBeFalse();
        (await result.Budgets.AnyAsync(x => seed.BudgetIds.Contains(x.Id))).ShouldBeFalse();
        (await result.Funds.AnyAsync(x => seed.FundIds.Contains(x.Id))).ShouldBeFalse();
        (await result.FundTransactions.AnyAsync(x => seed.FundIds.Contains(x.FundId))).ShouldBeFalse();
        (await result.Allocations.AnyAsync(x => x.LedgerId == seed.LedgerId)).ShouldBeFalse();
        (await result.AllocationTemplates.AnyAsync(x => seed.BudgetIds.Contains(x.BudgetId))).ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteUserAsync_WhenOtherUsersExist_ShouldKeepTheirData()
    {
        // Arrange
        var deleted = await SeedAsync();
        var kept = await SeedAsync();
        var store = new EntityStore(GetContext());

        // Act
        await store.RunInTransactionAsync(() => store.DeleteUserAsync(deleted.UserId));

        // Assert
        var result = GetContext();
        (await result.Users.AnyAsync(x => x.Id == kept.UserId)).ShouldBeTrue();
        (await result.Ledgers.AnyAsync(x => x.Id == kept.LedgerId)).ShouldBeTrue();
        (await result.Accounts.CountAsync(x => kept.AccountIds.Contains(x.Id))).ShouldBe(2);
        (await result.AccountTransactions.CountAsync(x => kept.TransactionIds.Contains(x.Id))).ShouldBe(kept.TransactionIds.Length);
        (await result.AccountTransfers.AnyAsync(x => kept.TransactionIds.Contains(x.IncomeId))).ShouldBeTrue();
        (await result.FundTransactions.CountAsync(x => kept.FundIds.Contains(x.FundId))).ShouldBe(4);
        (await result.Allocations.AnyAsync(x => x.LedgerId == kept.LedgerId)).ShouldBeTrue();
        (await result.AllocationTemplates.AnyAsync(x => kept.BudgetIds.Contains(x.BudgetId))).ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteUserAsync_WhenUserHasNoLedgers_ShouldDeleteUser()
    {
        // Arrange
        var user = new User { Email = $"test@email{Guid.NewGuid()}", HashedPassword = "Test Hashed Password" };
        var dbContext = GetContext();
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(CancellationToken.None);
        var store = new EntityStore(GetContext());

        // Act
        await store.RunInTransactionAsync(() => store.DeleteUserAsync(user.Id));

        // Assert
        (await GetContext().Users.AnyAsync(x => x.Id == user.Id)).ShouldBeFalse();
    }

    private async Task<Seed> SeedAsync()
    {
        var user = new User { Name = "Test User", Email = $"test@email{Guid.NewGuid()}", HashedPassword = "Test Hashed Password" };
        var ledger = new Ledger { OwnerId = user.Id, Name = $"Ledger {Guid.NewGuid()}" };
        var cash = new Account { LedgerId = ledger.Id, Name = "Cash" };
        var bank = new Account { LedgerId = ledger.Id, Name = "Bank" };
        var budget = new Budget { LedgerId = ledger.Id, Name = "Budget" };
        var fund = budget.AddFund("Food");
        budget.AddAllocationTemplate("Template", "EUR", [new AllocationLine(fund.Id, 0, AllocationType.Fixed, 10, null)]);

        var income = AccountTransaction.Create(cash.Id, new Money(100, "EUR"), DateTimeOffset.UtcNow, "Salary");
        var incomeAllocation = FundTransaction.Allocate(fund.Id, new Money(40, "EUR"), income);
        var expense = AccountTransaction.Create(cash.Id, new Money(-20, "EUR"), DateTimeOffset.UtcNow, "Lunch");
        var payment = FundTransaction.Pay(fund.Id, expense);
        var transfer = AccountTransfer.Transfer(cash.Id, bank.Id, new Money(10, "EUR"), DateTimeOffset.UtcNow, "Transfer");
        var allocation = Allocation.Create(ledger.Id, "EUR", DateTimeOffset.UtcNow, [new FundAmount(fund.Id, 5)]);
        var initial = Allocation.CreateInitial(ledger.Id, "EUR", [new FundAmount(fund.Id, 7)]);

        var dbContext = GetContext();
        dbContext.AddRange(user, ledger, cash, bank, budget, income, incomeAllocation, expense, payment, transfer, allocation, initial);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        return new Seed(
            user.Id,
            ledger.Id,
            [cash.Id, bank.Id],
            [budget.Id],
            [fund.Id],
            [income.Id, expense.Id, transfer.Income.Id, transfer.Expense.Id]);
    }
}
