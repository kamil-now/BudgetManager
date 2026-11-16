using BudgetManager.Application.Commands;
using BudgetManager.Application.Models;
using BudgetManager.Application.Queries;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Models;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Application;

public class GetLedgerFeedTests(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    private static readonly DateTimeOffset Today = new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetLedgerFeed_WhenLedgerHasEveryKindOfItem_ShouldReturnOneItemOfEachTypeNewestFirst()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var cash = ledger.Accounts.Single().Id;
        var card = await Mediator.Send(new CreateAccountCommand(ledger.Id, new(0, "EUR"), "Card"));
        var income = await IncomeAsync(cash, 100, Today.AddDays(-5));
        var expense = await IncomeAsync(cash, -40, Today.AddDays(-4));
        var transfer = await Mediator.Send(new CreateAccountTransferCommand(cash, card, new(10, "EUR"), Today.AddDays(-3), "Move"));
        var exchange = await Mediator.Send(new CreateCurrencyExchangeCommand(cash, null, new(11, "USD"), new(10, "EUR"), Today.AddDays(-2), "Exchange"));
        var allocation = await StandaloneAsync(FundId(ledger, "Food"), 20, Today.AddDays(-1));

        // Act
        var result = await Mediator.Send(new GetLedgerFeedQuery(ledger.Id, new()));

        // Assert
        result.Items.Select(x => (x.Id, x.Type)).ShouldBe([
            (allocation, FeedItemType.Allocation),
            (exchange, FeedItemType.Exchange),
            (transfer, FeedItemType.Transfer),
            (expense, FeedItemType.Expense),
            (income, FeedItemType.Income)]);
    }

    [Fact]
    public async Task GetLedgerFeed_WhenTypesAreSet_ShouldReturnOnlyItemsOfThoseTypes()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var cash = ledger.Accounts.Single().Id;
        var income = await IncomeAsync(cash, 100);
        await IncomeAsync(cash, -40);
        var allocation = await StandaloneAsync(FundId(ledger, "Food"), 20);

        // Act
        var result = await Mediator.Send(new GetLedgerFeedQuery(ledger.Id, new() { Types = [FeedItemType.Income, FeedItemType.Allocation] }));

        // Assert
        result.Items.Select(x => x.Id).ShouldBe([allocation, income], ignoreOrder: true);
    }

    [Theory]
    [InlineData("groceries")]
    [InlineData("GROCER")]
    [InlineData("cash")]
    public async Task GetLedgerFeed_WhenSearchMatchesTitleOrAccount_ShouldReturnMatchingItemsIgnoringCase(string search)
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var cash = ledger.Accounts.Single().Id;
        var card = await Mediator.Send(new CreateAccountCommand(ledger.Id, new(0, "EUR"), "Card"));
        var groceries = await Mediator.Send(new CreateAccountTransactionCommand(cash, new(-30, "EUR"), Today, "Groceries"));
        await Mediator.Send(new CreateAccountTransactionCommand(card, new(-10, "EUR"), Today, "Bus"));

        // Act
        var result = await Mediator.Send(new GetLedgerFeedQuery(ledger.Id, new() { Search = search }));

        // Assert
        result.Items.ShouldHaveSingleItem().Id.ShouldBe(groceries);
    }

    [Fact]
    public async Task GetLedgerFeed_WhenSearchMatchesFundName_ShouldReturnItsAllocations()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");
        await IncomeAsync(ledger.Accounts.Single().Id, 100);
        var rent = await StandaloneAsync(FundId(ledger, "Rent"), 20);
        await StandaloneAsync(FundId(ledger, "Food"), 20);

        // Act
        var result = await Mediator.Send(new GetLedgerFeedQuery(ledger.Id, new() { Search = "ren" }));

        // Assert
        result.Items.ShouldHaveSingleItem().Id.ShouldBe(rent);
    }

    [Fact]
    public async Task GetLedgerFeed_WhenSearchHasLikeWildcards_ShouldMatchThemLiterally()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync();
        var cash = ledger.Accounts.Single().Id;
        var discount = await Mediator.Send(new CreateAccountTransactionCommand(cash, new(-30, "EUR"), Today, "50% off"));
        await Mediator.Send(new CreateAccountTransactionCommand(cash, new(-10, "EUR"), Today, "50 off"));

        // Act
        var result = await Mediator.Send(new GetLedgerFeedQuery(ledger.Id, new() { Search = "0%" }));

        // Assert
        result.Items.ShouldHaveSingleItem().Id.ShouldBe(discount);
    }

    [Fact]
    public async Task GetLedgerFeed_WhenAccountIdIsSet_ShouldReturnOnlyAccountItemsTouchingIt()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var cash = ledger.Accounts.Single().Id;
        var card = await Mediator.Send(new CreateAccountCommand(ledger.Id, new(0, "EUR"), "Card"));
        var savings = await Mediator.Send(new CreateAccountCommand(ledger.Id, new(0, "EUR"), "Savings"));
        await IncomeAsync(cash, 100);
        var cardIncome = await IncomeAsync(card, 50);
        var intoCard = await Mediator.Send(new CreateAccountTransferCommand(cash, card, new(10, "EUR"), Today, "Into card"));
        var outOfCard = await Mediator.Send(new CreateAccountTransferCommand(card, savings, new(5, "EUR"), Today, "Out of card"));
        await Mediator.Send(new CreateAccountTransferCommand(cash, savings, new(5, "EUR"), Today, "Elsewhere"));
        await StandaloneAsync(FundId(ledger, "Food"), 20);

        // Act
        var result = await Mediator.Send(new GetLedgerFeedQuery(ledger.Id, new() { AccountId = card }));

        // Assert
        result.Items.Select(x => x.Id).ShouldBe([cardIncome, intoCard, outOfCard], ignoreOrder: true);
    }

    [Fact]
    public async Task GetLedgerFeed_WhenBudgetIdIsSet_ShouldReturnItsFundItemsAndIncomesAllocatedIntoIt()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var seeded = await SeedLedgerAsync("Food");
        await Mediator.Send(new CreateBudgetCommand(seeded.Id, "Other", [new CreateFundDTO("Travel")]));
        var ledger = await Mediator.Send(new GetLedgerQuery(seeded.Id));
        var cash = ledger.Accounts.Single().Id;
        var budget = ledger.Budgets.Single(x => x.Name == "Budget").Id;
        var budgetTemplate = await TemplateAsync(ledger, "Budget", "Food");
        var otherTemplate = await TemplateAsync(ledger, "Other", "Travel");
        var allocatedToBudget = await IncomeAsync(cash, 100, allocation: new AllocationInput(TemplateId: budgetTemplate));
        await IncomeAsync(cash, 100, allocation: new AllocationInput(TemplateId: otherTemplate));
        await IncomeAsync(cash, 100);
        await IncomeAsync(cash, -10);
        var standalone = await StandaloneAsync(FundId(ledger, "Food"), 20);
        await StandaloneAsync(FundId(ledger, "Travel"), 20);

        // Act
        var result = await Mediator.Send(new GetLedgerFeedQuery(ledger.Id, new() { BudgetId = budget }));

        // Assert
        result.Items.Select(x => x.Id).ShouldBe([allocatedToBudget, standalone], ignoreOrder: true);
    }

    [Fact]
    public async Task GetLedgerFeed_WhenFundIdIsSet_ShouldReturnItsFundItemsAndIncomesAllocatedIntoIt()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");
        var cash = ledger.Accounts.Single().Id;
        var template = await TemplateAsync(ledger, "Budget", "Food");
        var allocatedToFood = await IncomeAsync(cash, 100, allocation: new AllocationInput(TemplateId: template));
        var food = await StandaloneAsync(FundId(ledger, "Food"), 20);
        await StandaloneAsync(FundId(ledger, "Rent"), 20);

        // Act
        var result = await Mediator.Send(new GetLedgerFeedQuery(ledger.Id, new() { FundId = FundId(ledger, "Food") }));

        // Assert
        result.Items.Select(x => x.Id).ShouldBe([allocatedToFood, food], ignoreOrder: true);
    }

    [Fact]
    public async Task GetLedgerFeed_WhenFundBelongsToAnotherBudgetThanBudgetId_ShouldReturnNothing()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var seeded = await SeedLedgerAsync("Food");
        await Mediator.Send(new CreateBudgetCommand(seeded.Id, "Other", [new CreateFundDTO("Travel")]));
        var ledger = await Mediator.Send(new GetLedgerQuery(seeded.Id));
        await StandaloneAsync(FundId(ledger, "Food"), 20);
        var other = ledger.Budgets.Single(x => x.Name == "Other").Id;

        // Act
        var result = await Mediator.Send(new GetLedgerFeedQuery(ledger.Id, new() { BudgetId = other, FundId = FundId(ledger, "Food") }));

        // Assert
        result.ShouldSatisfyAllConditions(
            x => x.Items.ShouldBeEmpty(),
            x => x.Total.ShouldBe(0));
    }

    [Fact]
    public async Task GetLedgerFeed_WhenIncomeIsAllocated_ShouldNestItsAllocationsInsteadOfListingThem()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");
        var cash = ledger.Accounts.Single().Id;
        var budget = ledger.Budgets.Single(x => x.Name == "Budget").Id;
        var template = await TemplateAsync(ledger, "Budget", "Food", "Rent");
        var income = await IncomeAsync(cash, 100, allocation: new AllocationInput(TemplateId: template));

        // Act
        var result = await Mediator.Send(new GetLedgerFeedQuery(ledger.Id, new()));

        // Assert
        var item = result.Items.ShouldHaveSingleItem();
        item.ShouldSatisfyAllConditions(
            x => x.Id.ShouldBe(income),
            x => x.Allocated.ShouldBe(100),
            x => x.Allocations.Select(a => (a.FundName, a.BudgetId, a.BudgetName, a.Amount)).ShouldBe([
                ("Food", budget, "Budget", new Money(50, "EUR")),
                ("Rent", budget, "Budget", new Money(50, "EUR"))], ignoreOrder: true));
    }

    [Fact]
    public async Task GetLedgerFeed_WhenExpenseIsPaidFromFund_ShouldCarryTheFundAndItsBudget()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var expense = await PaidAsync(ledger.Accounts.Single().Id, FundId(ledger, "Food"), 30);

        // Act
        var result = await Mediator.Send(new GetLedgerFeedQuery(ledger.Id, new()));

        // Assert
        var item = result.Items.ShouldHaveSingleItem();
        item.ShouldSatisfyAllConditions(
            x => x.Id.ShouldBe(expense),
            x => x.Type.ShouldBe(FeedItemType.Expense),
            x => x.FundId.ShouldBe(FundId(ledger, "Food")),
            x => x.FundName.ShouldBe("Food"),
            x => x.BudgetId.ShouldBe(ledger.Budgets.Single().Id),
            x => x.BudgetName.ShouldBe("Budget"),
            x => x.Allocated.ShouldBe(0),
            x => x.Allocations.ShouldBeEmpty());
    }

    [Fact]
    public async Task GetLedgerFeed_WhenExpenseIsNotPaidFromFund_ShouldHaveNoFund()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        await IncomeAsync(ledger.Accounts.Single().Id, -30);

        // Act
        var result = await Mediator.Send(new GetLedgerFeedQuery(ledger.Id, new()));

        // Assert
        result.Items.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            x => x.FundId.ShouldBeNull(),
            x => x.FundName.ShouldBeNull(),
            x => x.BudgetId.ShouldBeNull());
    }

    [Fact]
    public async Task GetLedgerFeed_WhenFundIdIsSet_ShouldReturnExpensesPaidFromIt()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");
        var cash = ledger.Accounts.Single().Id;
        var paidFromFood = await PaidAsync(cash, FundId(ledger, "Food"), 30);
        await PaidAsync(cash, FundId(ledger, "Rent"), 40);
        await IncomeAsync(cash, -5);

        // Act
        var result = await Mediator.Send(new GetLedgerFeedQuery(ledger.Id, new() { FundId = FundId(ledger, "Food") }));

        // Assert
        result.Items.ShouldHaveSingleItem().Id.ShouldBe(paidFromFood);
    }

    [Fact]
    public async Task GetLedgerFeed_WhenBudgetIdIsSet_ShouldReturnExpensesPaidFromItsFunds()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var seeded = await SeedLedgerAsync("Food");
        await Mediator.Send(new CreateBudgetCommand(seeded.Id, "Other", [new CreateFundDTO("Travel")]));
        var ledger = await Mediator.Send(new GetLedgerQuery(seeded.Id));
        var cash = ledger.Accounts.Single().Id;
        var paid = await PaidAsync(cash, FundId(ledger, "Food"), 30);
        await PaidAsync(cash, FundId(ledger, "Travel"), 40);

        // Act
        var result = await Mediator.Send(new GetLedgerFeedQuery(ledger.Id, new() { BudgetId = ledger.Budgets.Single(x => x.Name == "Budget").Id }));

        // Assert
        result.Items.ShouldHaveSingleItem().Id.ShouldBe(paid);
    }

    [Fact]
    public async Task GetLedgerFeed_WhenSearchMatchesFundNameOfPaidExpense_ShouldReturnIt()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");
        var cash = ledger.Accounts.Single().Id;
        var paid = await PaidAsync(cash, FundId(ledger, "Rent"), 30);
        await PaidAsync(cash, FundId(ledger, "Food"), 40);

        // Act
        var result = await Mediator.Send(new GetLedgerFeedQuery(ledger.Id, new() { Search = "ren" }));

        // Assert
        result.Items.ShouldHaveSingleItem().Id.ShouldBe(paid);
    }

    [Fact]
    public async Task GetLedgerFeed_WhenAllocationHasSeveralFunds_ShouldReturnOneItemWithTheLines()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");
        var allocation = await AllocationAsync([new(FundId(ledger, "Food"), 20), new(FundId(ledger, "Rent"), 30)]);

        // Act
        var result = await Mediator.Send(new GetLedgerFeedQuery(ledger.Id, new()));

        // Assert
        var item = result.Items.ShouldHaveSingleItem();
        item.ShouldSatisfyAllConditions(
            x => x.Id.ShouldBe(allocation),
            x => x.Type.ShouldBe(FeedItemType.Allocation),
            x => x.Value.ShouldBe(new Money(50, "EUR")),
            x => x.BudgetName.ShouldBe("Budget"),
            x => x.FundName.ShouldBe("Food, Rent"),
            x => x.IsInitialBalance.ShouldBeFalse(),
            x => x.Allocations.Select(a => (a.FundName, a.Amount)).ShouldBe([
                ("Food", new Money(20, "EUR")),
                ("Rent", new Money(30, "EUR"))], ignoreOrder: true));
    }

    [Fact]
    public async Task GetLedgerFeed_WhenFundIdIsSet_ShouldReturnAllocationsWithALineInIt()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent", "Fun");
        var both = await AllocationAsync([new(FundId(ledger, "Food"), 20), new(FundId(ledger, "Rent"), 30)]);
        await AllocationAsync([new(FundId(ledger, "Fun"), 10)]);

        // Act
        var result = await Mediator.Send(new GetLedgerFeedQuery(ledger.Id, new() { FundId = FundId(ledger, "Rent") }));

        // Assert
        var item = result.Items.ShouldHaveSingleItem();
        item.Id.ShouldBe(both);
        item.Allocations.Count().ShouldBe(2);
    }

    [Fact]
    public async Task GetLedgerFeed_WhenLedgerHasInitialAllocation_ShouldReturnItMarkedAsOpeningRow()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledgerId = await Mediator.Send(new CreateLedgerCommand(
            $"Ledger {Guid.NewGuid()}",
            [new CreateBudgetDTO("Budget", [new CreateFundDTO("Food"), new CreateFundDTO("Rent")])],
            [new CreateAccountDTO(new(100, "EUR"), "Cash")],
            [new InitialAllocationDTO("Budget", "Food", new(60, "EUR")), new InitialAllocationDTO("Budget", "Rent", new(40, "EUR"))]));

        // Act
        var result = await Mediator.Send(new GetLedgerFeedQuery(ledgerId, new() { Types = [FeedItemType.Allocation] }));

        // Assert
        result.Items.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            x => x.IsInitialBalance.ShouldBeTrue(),
            x => x.Value.ShouldBe(new Money(100, "EUR")),
            x => x.Allocations.Count().ShouldBe(2));
    }

    [Fact]
    public async Task GetLedgerFeed_WhenIncomeIsNotAllocated_ShouldHaveNoAllocations()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync();
        await IncomeAsync(ledger.Accounts.Single().Id, 100);

        // Act
        var result = await Mediator.Send(new GetLedgerFeedQuery(ledger.Id, new()));

        // Assert
        result.Items.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            x => x.Allocated.ShouldBe(0),
            x => x.Allocations.ShouldBeEmpty());
    }

    [Fact]
    public async Task GetLedgerFeed_WhenMoneyIsTransferred_ShouldReturnTransferFromSourceToTarget()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync();
        var cash = ledger.Accounts.Single().Id;
        var card = await Mediator.Send(new CreateAccountCommand(ledger.Id, new(0, "EUR"), "Card"));
        var transfer = await Mediator.Send(new CreateAccountTransferCommand(cash, card, new(50, "EUR"), Today, "Move"));

        // Act
        var result = await Mediator.Send(new GetLedgerFeedQuery(ledger.Id, new()));

        // Assert
        result.Items.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            x => x.Id.ShouldBe(transfer),
            x => x.Type.ShouldBe(FeedItemType.Transfer),
            x => x.AccountId.ShouldBe(cash),
            x => x.AccountName.ShouldBe("Cash"),
            x => x.TargetAccountId.ShouldBe(card),
            x => x.TargetAccountName.ShouldBe("Card"),
            x => x.Value.ShouldBe(new Money(50, "EUR")),
            x => x.Received.ShouldBeNull(),
            x => x.Title.ShouldBe("Move"));
    }

    [Fact]
    public async Task GetLedgerFeed_WhenCurrencyIsExchanged_ShouldReturnExchangeWithSentAndReceived()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync();
        var cash = ledger.Accounts.Single().Id;
        var exchange = await Mediator.Send(new CreateCurrencyExchangeCommand(cash, null, new(110, "USD"), new(100, "EUR"), Today, "Exchange"));

        // Act
        var result = await Mediator.Send(new GetLedgerFeedQuery(ledger.Id, new()));

        // Assert
        result.Items.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            x => x.Id.ShouldBe(exchange),
            x => x.Type.ShouldBe(FeedItemType.Exchange),
            x => x.AccountId.ShouldBe(cash),
            x => x.TargetAccountId.ShouldBe(cash),
            x => x.Value.ShouldBe(new Money(100, "EUR")),
            x => x.Received.ShouldBe(new Money(110, "USD")));
    }

    [Fact]
    public async Task GetLedgerFeed_WhenTransactionIsInitialBalance_ShouldReturnItAsMarkedIncome()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync();
        await Mediator.Send(new CreateAccountCommand(ledger.Id, new(50, "EUR"), "Card"));

        // Act
        var result = await Mediator.Send(new GetLedgerFeedQuery(ledger.Id, new()));

        // Assert
        result.Items.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            x => x.Type.ShouldBe(FeedItemType.Income),
            x => x.IsInitialBalance.ShouldBeTrue(),
            x => x.Value.ShouldBe(new Money(50, "EUR")));
    }

    [Fact]
    public async Task GetLedgerFeed_WhenFromAndToAreSet_ShouldReturnOnlyItemsInRange()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync();
        var cash = ledger.Accounts.Single().Id;
        await IncomeAsync(cash, 10, Today.AddDays(-10));
        var inRange = await IncomeAsync(cash, 20, Today);
        await IncomeAsync(cash, 30, Today.AddDays(10));

        // Act
        var result = await Mediator.Send(new GetLedgerFeedQuery(ledger.Id, new() { From = Today.AddDays(-1), To = Today.AddDays(1) }));

        // Assert
        result.Items.ShouldHaveSingleItem().Id.ShouldBe(inRange);
    }

    [Fact]
    public async Task GetLedgerFeed_WhenOffsetAndLimitAreSet_ShouldReturnThatPageWithTheTotal()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync();
        var cash = ledger.Accounts.Single().Id;
        var ids = new List<Guid>();
        for (var day = 0; day < 5; day++)
        {
            ids.Add(await IncomeAsync(cash, 10 + day, Today.AddDays(-day)));
        }

        // Act
        var result = await Mediator.Send(new GetLedgerFeedQuery(ledger.Id, new() { Offset = 1, Limit = 2 }));

        // Assert
        result.ShouldSatisfyAllConditions(
            x => x.Items.Select(i => i.Id).ShouldBe([ids[1], ids[2]]),
            x => x.Total.ShouldBe(5),
            x => x.Offset.ShouldBe(1),
            x => x.Limit.ShouldBe(2));
    }

    [Fact]
    public async Task GetLedgerFeed_WhenItemsShareTheirDate_ShouldPageThemInAStableOrder()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var cash = ledger.Accounts.Single().Id;
        for (var i = 0; i < 4; i++)
        {
            await IncomeAsync(cash, 10 + i);
            await StandaloneAsync(FundId(ledger, "Food"), 1 + i);
        }
        var all = await Mediator.Send(new GetLedgerFeedQuery(ledger.Id, new()));

        // Act
        var pages = new List<Guid>();
        for (var offset = 0; offset < all.Total; offset += 3)
        {
            var page = await Mediator.Send(new GetLedgerFeedQuery(ledger.Id, new() { Offset = offset, Limit = 3 }));
            pages.AddRange(page.Items.Select(x => x.Id));
        }

        // Assert
        pages.ShouldBe(all.Items.Select(x => x.Id));
    }

    [Fact]
    public async Task GetLedgerFeed_WhenFromIsLaterThanTo_ShouldThrowValidationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync();

        // Act & Assert
        await Should.ThrowAsync<ValidationException>(() =>
            Mediator.Send(new GetLedgerFeedQuery(ledger.Id, new() { From = Today, To = Today.AddDays(-1) })));
    }

    public sealed class InvalidFilters : TheoryData<LedgerFeedFilters, string>
    {
        public InvalidFilters()
        {
            Add(new() { Offset = -1 }, "Offset must be greater than or equal zero.");
            Add(new() { Limit = 0 }, $"Limit must be between 1 and {Constants.MaxFeedLimit}.");
            Add(new() { Limit = Constants.MaxFeedLimit + 1 }, $"Limit must be between 1 and {Constants.MaxFeedLimit}.");
            Add(new() { Search = new string('a', Constants.MaxSearchLength + 1) }, $"Search value is too long. Max length is {Constants.MaxSearchLength}.");
            Add(new() { Types = [(FeedItemType)9] }, "Type '9' is not valid.");
        }
    }

    [Theory]
    [ClassData(typeof(InvalidFilters))]
    public async Task GetLedgerFeed_WhenFiltersAreInvalid_ShouldThrowValidationException(LedgerFeedFilters filters, string expectedError)
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync();

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(new GetLedgerFeedQuery(ledger.Id, filters)));
        ex.Message.ShouldBe(expectedError);
    }

    [Fact]
    public async Task GetLedgerFeed_WhenLedgerBelongsToAnotherUser_ShouldThrowAuthorizationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync();

        await MockAuthenticatedUserAsync();

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() => Mediator.Send(new GetLedgerFeedQuery(ledger.Id, new())));
    }

    private async Task<Guid> IncomeAsync(Guid accountId, decimal amount, DateTimeOffset? date = null, AllocationInput? allocation = null)
        => await Mediator.Send(new CreateAccountTransactionCommand(accountId, new(amount, "EUR"), date ?? Today, $"Transaction {amount}", Allocation: allocation));

    private async Task<Guid> StandaloneAsync(Guid fundId, decimal amount, DateTimeOffset? date = null)
        => await AllocationAsync([new FundAmount(fundId, amount)], date);

    private async Task<Guid> AllocationAsync(FundAmount[] amounts, DateTimeOffset? date = null)
    {
        var fund = await EntityStore.GetAsync<Fund>(amounts[0].FundId, [x => x.Budget]);
        var allocation = await EntityStore.CreateAsync(Allocation.Create(fund.Budget.LedgerId, "EUR", date ?? Today, amounts));
        await EntityStore.SaveChangesAsync();
        return allocation.Id;
    }

    private async Task<Guid> PaidAsync(Guid accountId, Guid fundId, decimal amount, DateTimeOffset? date = null)
        => await Mediator.Send(new CreateAccountTransactionCommand(accountId, new(-amount, "EUR"), date ?? Today, $"Paid {amount}", FundId: fundId));

    /// <summary>
    /// A EUR template of the budget that splits evenly between <paramref name="funds"/> (up to two); returns the template id.
    /// </summary>
    private Task<Guid> TemplateAsync(LedgerDTO ledger, string budget, params string[] funds)
        => CreateTemplateAsync(
            ledger.Budgets.Single(x => x.Name == budget).Id,
            "Template",
            "EUR",
            [.. funds.Select((x, i) => new AllocationLine(FundId(ledger, x), i, AllocationType.Percent, Percent: i == funds.Length - 1 ? 100 : 100m / (funds.Length - i)))]);
}
