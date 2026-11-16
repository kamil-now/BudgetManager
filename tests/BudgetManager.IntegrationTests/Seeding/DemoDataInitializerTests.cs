using BudgetManager.Application.Interfaces;
using BudgetManager.Application.Models;
using BudgetManager.Application.Queries;
using BudgetManager.Common;
using BudgetManager.DemoDataInitializer;
using BudgetManager.Domain;
using BudgetManager.Domain.Enums;
using BudgetManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BudgetManager.IntegrationTests.Seeding;

using DemoDataInitializer = BudgetManager.DemoDataInitializer.DemoDataInitializer;

public class DemoDataInitializerTests(DemoDataFixture fixture) : IClassFixture<DemoDataFixture>
{
    [Fact]
    public void InitializeAsync_WhenDatabaseIsEmpty_ShouldCreateData()
    {
        // Assert
        fixture.FirstRunInitialized.ShouldBeTrue();
    }

    [Fact]
    public async Task InitializeAsync_WhenUserExists_ShouldCreateNothing()
    {
        // Arrange
        await using var scope = fixture.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var counts = await CountsAsync(dbContext);

        // Act
        var initialized = await scope.ServiceProvider.GetRequiredService<DemoDataInitializer>().InitializeAsync();

        // Assert
        initialized.ShouldBeFalse();
        (await CountsAsync(dbContext)).ShouldBe(counts);
    }

    [Fact]
    public async Task InitializeAsync_ShouldCreateOneUserWithTheDocumentedCredentials()
    {
        // Arrange
        await using var scope = fixture.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Act
        var users = await dbContext.Users.AsNoTracking().ToListAsync();

        // Assert
        var user = users.ShouldHaveSingleItem();
        user.Email.ShouldBe("test@example.com");
        scope.ServiceProvider.GetRequiredService<IPasswordHasher>().Verify("testtest", user.HashedPassword).ShouldBeTrue();
    }

    [Fact]
    public async Task InitializeAsync_ShouldCreatePersonalAndBusinessLedgers()
    {
        // Arrange
        await using var scope = await fixture.CreateSignedInScopeAsync();

        // Act
        var ledgers = await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new GetLedgersQuery());

        // Assert
        ledgers.Select(x => x.Name).ShouldBe(["Business", "Personal"]);
    }

    [Theory]
    [InlineData("Personal", new[] { "Goals", "Household", "Lifestyle" })]
    [InlineData("Business", new[] { "Growth", "Operations", "Taxes" })]
    public async Task InitializeAsync_ShouldCreateThreeBudgetsInEveryLedger(string ledgerName, string[] budgets)
    {
        // Arrange
        await using var scope = await fixture.CreateSignedInScopeAsync();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var ledgerId = (await mediator.Send(new GetLedgersQuery())).Single(x => x.Name == ledgerName).Id;

        // Act
        var ledger = await mediator.Send(new GetLedgerQuery(ledgerId));

        // Assert
        ledger.Budgets.Select(x => x.Name).Order().ShouldBe(budgets);
    }

    [Fact]
    public async Task InitializeAsync_ShouldUseOnlyEur()
    {
        // Arrange
        await using var scope = fixture.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Act
        var currencies = await dbContext.AccountTransactions.Select(x => x.Value.Currency).Distinct().ToListAsync();

        // Assert
        currencies.ShouldBe(["EUR"]);
    }

    [Fact]
    public async Task InitializeAsync_ShouldCreateHistoryOfEveryKind()
    {
        // Arrange
        var feed = await PersonalFeedAsync();

        // Assert
        feed.Select(x => x.Type).Distinct()
            .ShouldBe(Enum.GetValues<FeedItemType>().Where(x => x != FeedItemType.Exchange), ignoreOrder: true);
    }

    [Fact]
    public async Task InitializeAsync_ShouldCreateAllocatedAndUnallocatedIncome()
    {
        // Arrange
        var feed = await PersonalFeedAsync();

        // Assert
        feed.ShouldContain(x => x.Type == FeedItemType.Income && !x.IsInitialBalance && x.Allocations.Any());
        feed.ShouldContain(x => x.Type == FeedItemType.Income && !x.IsInitialBalance && !x.Allocations.Any());
    }

    [Fact]
    public async Task InitializeAsync_ShouldPayExpensesFromFunds()
    {
        // Arrange
        var feed = await PersonalFeedAsync();

        // Assert
        feed.ShouldContain(x => x.Type == FeedItemType.Expense && x.FundId != null);
    }

    [Fact]
    public async Task InitializeAsync_ShouldNotDateAnythingInTheFuture()
    {
        // Arrange
        var feed = await PersonalFeedAsync();

        // Assert
        feed.ShouldAllBe(x => x.Date <= DateTimeOffset.UtcNow);
    }

    private async Task<IReadOnlyList<FeedItemDTO>> PersonalFeedAsync()
    {
        await using var scope = await fixture.CreateSignedInScopeAsync();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var personal = (await mediator.Send(new GetLedgersQuery())).Single(x => x.Name == "Personal");

        var items = new List<FeedItemDTO>();
        LedgerFeedPageDTO page;
        do
        {
            page = await mediator.Send(new GetLedgerFeedQuery(personal.Id, new() { Limit = Constants.MaxFeedLimit, Offset = items.Count }));
            items.AddRange(page.Items);
        }
        while (page.Items.Count > 0 && items.Count < page.Total);

        return items;
    }

    private static async Task<(int Users, int Ledgers, int AccountTransactions, int FundTransactions, int Transfers, int Allocations)> CountsAsync(ApplicationDbContext dbContext)
        => (await dbContext.Users.CountAsync(),
            await dbContext.Ledgers.CountAsync(),
            await dbContext.AccountTransactions.CountAsync(),
            await dbContext.FundTransactions.CountAsync(),
            await dbContext.AccountTransfers.CountAsync(),
            await dbContext.Allocations.CountAsync());
}
