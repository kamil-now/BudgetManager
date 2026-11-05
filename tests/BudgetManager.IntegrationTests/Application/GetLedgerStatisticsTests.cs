using BudgetManager.Application.Commands;
using BudgetManager.Application.Queries;
using BudgetManager.Domain.Entities;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Application;

public class GetLedgerStatisticsTests(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task GetLedgerStatistics_WhenTransferIsWithinLedger_ShouldExcludeIt()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var source = await CreateAccountAsync(userId);
        var target = await CreateAccountAsync(userId, source.LedgerId);
        await Mediator.Send(new CreateAccountTransferCommand(source.Id, target.Id, new(100, "PLN"), DateTimeOffset.UtcNow, null));

        // Act
        var statistics = await Mediator.Send(new GetLedgerStatisticsQuery(source.LedgerId, new()));

        // Assert
        statistics.ShouldNotBeNull();
        statistics.TotalIncome.ShouldBeEmpty();
        statistics.TotalExpense.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetLedgerStatistics_WhenTransferLeavesLedger_ShouldCountExpenseInSourceLedger()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var source = await CreateAccountAsync(userId);
        var target = await CreateAccountAsync(userId);
        await Mediator.Send(new CreateAccountTransferCommand(source.Id, target.Id, new(100, "PLN"), DateTimeOffset.UtcNow, null));

        // Act
        var statistics = await Mediator.Send(new GetLedgerStatisticsQuery(source.LedgerId, new()));

        // Assert
        statistics.ShouldNotBeNull();
        statistics.TotalExpense["PLN"].Total.ShouldBe(-100);
        statistics.TotalIncome.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetLedgerStatistics_WhenTransferEntersLedger_ShouldCountIncomeInTargetLedger()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var source = await CreateAccountAsync(userId);
        var target = await CreateAccountAsync(userId);
        await Mediator.Send(new CreateAccountTransferCommand(source.Id, target.Id, new(100, "PLN"), DateTimeOffset.UtcNow, null));

        // Act
        var statistics = await Mediator.Send(new GetLedgerStatisticsQuery(target.LedgerId, new()));

        // Assert
        statistics.ShouldNotBeNull();
        statistics.TotalIncome["PLN"].Total.ShouldBe(100);
        statistics.TotalExpense.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 1)]
    public async Task GetLedgerStatistics_WhenInitialBalanceIsRenamed_ShouldStillFilterItByKind(bool includeInitialBalance, int expectedIncomeCurrencies)
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var account = await CreateAccountAsync(userId);
        var initialBalance = AccountTransaction.CreateInitialBalance(account.Id, new(100, "PLN"));
        initialBalance.Update(account.Id, initialBalance.Value, initialBalance.Date, "renamed", null, null);
        await EntityStore.CreateAsync(initialBalance);
        await EntityStore.SaveChangesAsync();

        // Act
        var statistics = await Mediator.Send(new GetLedgerStatisticsQuery(account.LedgerId, new() { IncludeInitialBalance = includeInitialBalance }));

        // Assert
        statistics.ShouldNotBeNull();
        statistics.TotalIncome.Count.ShouldBe(expectedIncomeCurrencies);
    }

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
}
