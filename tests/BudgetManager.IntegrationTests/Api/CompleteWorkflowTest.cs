using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BudgetManager.Api.Models;
using BudgetManager.Application.Commands;
using BudgetManager.Application.Handlers;
using BudgetManager.Application.Models;
using BudgetManager.Common.Enums;
using BudgetManager.Common.Models;
using Shouldly;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace BudgetManager.IntegrationTests.Api;

/// <summary>
/// Serves as E2E test substitute by validating full user journey and cross-feature integration through the API layer
/// </summary>
/// 
[Collection("Sequential")]
[TestCaseOrderer("BudgetManager.IntegrationTests.Api.PriorityOrderer", "BudgetManager.IntegrationTests")]
public class CompleteWorkflowTest(ITestOutputHelper testOutputHelper, ApiFixture fixture, TestState state) : BaseTest(testOutputHelper, fixture), IClassFixture<TestState>
{
    private readonly TestState _testState = state;


    [Fact, TestPriority(1)]
    public async Task UserRegistration()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register", new CreateUserCommand(_testState.UserEmail, _testState.UserPassword, _testState.UserName));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var id = await response.Content.ReadFromJsonAsync<Guid?>();

        id.ShouldNotBeNull();
        id.ShouldNotBe(Guid.Empty);

        _testState.UserId = id;
    }

    [Fact, TestPriority(2)]
    public async Task UserLogin()
    {
        _testState.UserId.ShouldNotBeNull();

        var response = await Client.PostAsJsonAsync("/api/auth/login", new LoginCommand(_testState.UserEmail, _testState.UserPassword));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();

        tokenResponse.ShouldNotBeNull();

        tokenResponse.Token.ShouldNotBeEmpty();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse.Token);
    }

    [Fact, TestPriority(3)]
    public async Task CreateLedgerWithAccounts()
    {
        _testState.UserId.ShouldNotBeNull();

        var command = new CreateLedgerCommand("Default Ledger", null,
          new("Main budget", [
              new("Food", 0, 600, AllocationType.Fixed),
              new("Rent", 1, 800, AllocationType.Fixed),
              new("Utilities", 2, 200, AllocationType.Fixed),
              new("Entertainment", 3, 0.2m, AllocationType.Percent),
              new("Savings", 4, 0.8m, AllocationType.Percent)
              ]),
              [
                new(new(256, "EUR"), "Cash"),
                new(new(2048, "EUR"), "Main Account"),
                new(new(4096, "EUR"), "Savings Account")
              ]);
        var response = await Client.PostAsJsonAsync("/api/ledgers", command);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var id = await response.Content.ReadFromJsonAsync<Guid>();

        id.ShouldNotBe(Guid.Empty);
        _testState.LedgerId = id;
        _testState.CreatedLedger = command;
    }

    [Fact, TestPriority(4)]
    public async Task FetchLedgerSummary()
    {
        _testState.LedgerId.ShouldNotBeNull();
        _testState.CreatedLedger.ShouldNotBeNull();

        var response = await Client.GetAsync($"/api/ledgers/{_testState.LedgerId}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var ledger = await response.Content.ReadFromJsonAsync<LedgerDTO>();

        ledger.ShouldNotBeNull();

        ledger.Name.ShouldBe(_testState.CreatedLedger.Name);
        ledger.Description.ShouldBe(_testState.CreatedLedger.Description);

        ledger.Budgets.Count().ShouldBe(1);
        var budget = ledger.Budgets.First();

        budget.Name.ShouldBe(_testState.CreatedLedger.Budget.Name);
        budget.Description.ShouldBe(_testState.CreatedLedger.Budget.Description);

        budget.Funds.Count().ShouldBe(_testState.CreatedLedger.Budget.Funds.Count());

        foreach (var expectedFund in _testState.CreatedLedger.Budget.Funds)
        {
            var fund = budget.Funds.FirstOrDefault(x => x.Name == expectedFund.Name);
            fund.ShouldNotBeNull($"Fund with name '{expectedFund.Name}' not found.");
            fund.Description.ShouldBe(expectedFund.Description);
            fund.Balance.Keys.ShouldBeEmpty();
        }

        ledger.Accounts.Count().ShouldBe(_testState.CreatedLedger.Accounts.Count());

        foreach (var expectedAccount in _testState.CreatedLedger.Accounts)
        {
            var account = ledger.Accounts.FirstOrDefault(x => x.Name == expectedAccount.Name);
            account.ShouldNotBeNull($"Account with name '{expectedAccount.Name}' not found.");
            account.Description.ShouldBe(expectedAccount.Description);
            account.Balance.Keys.Count.ShouldBe(1);
            account.Balance.ContainsKey(expectedAccount.InitialBalance.Currency).ShouldBeTrue();
            account.Balance[expectedAccount.InitialBalance.Currency].ShouldBe(expectedAccount.InitialBalance.Amount);
        }

        _testState.Ledger = ledger;
    }

    [Fact, TestPriority(5)]
    public async Task CreateAccountTransactions()
    {
        _testState.Ledger.ShouldNotBeNull();
        var accounts = _testState.Ledger.Accounts.ToArray();
        accounts.Length.ShouldBe(3);

        async Task POST<T>(string url, T payload)
        {
            var response = await Client.PostAsJsonAsync(url, payload);
            response.StatusCode.ShouldBe(HttpStatusCode.Created, response.Content.ReadAsStringAsync().Result);
            var id = await response.Content.ReadFromJsonAsync<Guid>();
            id.ShouldNotBe(Guid.Empty);
        }

        var cashGift = new CreateAccountTransactionCommand(
            AccountId: accounts[0].Id,
            Value: new Money(100, "USD"),
            Date: DateTimeOffset.UtcNow.AddDays(-2),
            Title: "gift",
            Comment: "birthday",
            Tags: ["extra"]);

        var currencyExchange = new CreateCurrencyExchangeCommand(
            AccountId: accounts[0].Id,
            TargetAccountId: null,
            Buy: new Money(85.38m, "EUR"),
            Sell: new Money(100, "USD"),
            Date: DateTimeOffset.UtcNow.AddDays(-1),
            Title: null,
            Comment: null);

        var salary = new CreateAccountTransactionCommand(
            AccountId: accounts[1].Id,
            Value: new Money(3000.01234567890123456789m, "EUR"),
            Date: DateTimeOffset.UtcNow,
            Title: "salary",
            Comment: null,
            Tags: ["regular"]);

        var savingsTransfer = new CreateAccountTransferCommand(
            AccountId: accounts[1].Id,
            TargetAccountId: accounts[2].Id,
            Value: new Money(1000, "EUR"),
            Date: DateTimeOffset.UtcNow.AddDays(-1),
            Title: "monthly",
            Comment: "from salary");

        var rent = new CreateAccountTransactionCommand(
            AccountId: accounts[1].Id,
            Value: new Money(-1000, "EUR"),
            Date: DateTimeOffset.UtcNow,
            Title: "rent",
            Comment: null,
            Tags: ["regular"]);

        await POST($"/api/accounts/{accounts[0].Id}/transaction", cashGift);
        await POST($"/api/accounts/{accounts[0].Id}/currencyExchange", currencyExchange);

        await POST($"/api/accounts/{accounts[1].Id}/transaction", salary);
        await POST($"/api/accounts/{accounts[1].Id}/transfer", savingsTransfer);
        await POST($"/api/accounts/{accounts[1].Id}/transaction", rent);

        _testState.CreatedIncomes = [cashGift, salary];
        _testState.CreatedExpenses = [rent];
        _testState.CreatedTransfers = [savingsTransfer];
        _testState.CreatedCurrencyExchanges = [currencyExchange];
    }

    [Fact, TestPriority(6)]
    public async Task FetchLedgerTransactions()
    {
        _testState.CreatedIncomes.ShouldNotBeNull();
        _testState.CreatedExpenses.ShouldNotBeNull();
        _testState.CreatedTransfers.ShouldNotBeNull();
        _testState.CreatedCurrencyExchanges.ShouldNotBeNull();

        var response = await Client.GetAsync($"/api/ledgers/{_testState.LedgerId}/transactions");

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var transactions = await response.Content.ReadFromJsonAsync<LedgerTransactionsDTO>();

        transactions.ShouldNotBeNull();

        foreach (var expectedIncome in _testState.CreatedIncomes)
        {
            var transaction = transactions.Incomes.FirstOrDefault(x => x.Title == expectedIncome.Title);
            transaction.ShouldNotBeNull($"Income with title '{expectedIncome.Title}' not found.");
            transaction.Comment.ShouldBe(expectedIncome.Comment);
            transaction.Title.ShouldBe(expectedIncome.Title);
            transaction.Value.ShouldBe(expectedIncome.Value);
            transaction.Received.ShouldBeNull();
        }

        foreach (var expectedExpense in _testState.CreatedExpenses)
        {
            var transaction = transactions.Expenses.FirstOrDefault(x => x.Title == expectedExpense.Title);
            transaction.ShouldNotBeNull($"Expense with title '{expectedExpense.Title}' not found.");
            transaction.Comment.ShouldBe(expectedExpense.Comment);
            transaction.Title.ShouldBe(expectedExpense.Title);
            transaction.Value.ShouldBe(expectedExpense.Value);
            transaction.Received.ShouldBeNull();
        }

        foreach (var expectedTransfer in _testState.CreatedTransfers)
        {
            var transfer = transactions.Transfers.FirstOrDefault(x => x.Title == expectedTransfer.Title);
            transfer.ShouldNotBeNull($"Transfer with title '{expectedTransfer.Title}' not found.");
            transfer.Comment.ShouldBe(expectedTransfer.Comment);
            transfer.Title.ShouldBe(expectedTransfer.Title);
            transfer.Value.ShouldBe(expectedTransfer.Value);
            transfer.Received.ShouldBeNull();
        }

        foreach (var expectedCurrencyExchange in _testState.CreatedCurrencyExchanges)
        {
            var exchange = transactions.CurrencyExchanges.FirstOrDefault(x => x.Title == expectedCurrencyExchange.Title);
            exchange.ShouldNotBeNull($"Currency Exchange with title '{expectedCurrencyExchange.Title}' not found.");
            exchange.Comment.ShouldBe(expectedCurrencyExchange.Comment);
            exchange.Title.ShouldBe(expectedCurrencyExchange.Title);
            exchange.Value.ShouldBe(expectedCurrencyExchange.Sell);
            exchange.Received.ShouldBe(expectedCurrencyExchange.Buy);
        }
        _testState.LedgerTransactions = transactions;
    }

    [Fact, TestPriority(7)]
    public async Task UpdateIncome()
    {
        _testState.LedgerTransactions.ShouldNotBeNull();
        var income = _testState.LedgerTransactions.Incomes.First(x => x.Title == "salary");
        var request = new UpdateAccountTransactionCommand(
            income.Id,
            income.AccountId,
            new Money(3000, income.Value.Currency),
            income.Date.AddDays(-1),
            income.Title,
            "invoice XYZ",
            [.. income.Tags ?? [], "taxed"]);

        var response = await Client.PutAsJsonAsync($"/api/accounts/{income.AccountId}/transaction/{income.Id}", request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var transactionsResponse = await Client.GetAsync($"/api/ledgers/{_testState.LedgerId}/transactions");
        transactionsResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await transactionsResponse.Content.ReadAsStringAsync());

        var transactions = await transactionsResponse.Content.ReadFromJsonAsync<LedgerTransactionsDTO>();

        transactions.ShouldNotBeNull();

        var updated = transactions.Incomes.FirstOrDefault(x => x.Id == income.Id);

        updated.ShouldNotBeNull();
        updated.AccountId.ShouldBe(request.AccountId);
        updated.Value.ShouldBe(request.Amount);
        updated.Date.ShouldBe(request.Date);
        updated.Title.ShouldBe(request.Title);
        updated.Comment.ShouldBe(request.Comment);
        updated.Tags.ShouldBe(request.Tags);

        _testState.LedgerTransactions = transactions;
    }

#pragma warning disable CS1998, CA1822


    private async Task FetchLedgerStatistics()
    {
        // TODO
    }

    private async Task GenerateBudgetProposal()
    {
        // TODO
        /* based on 30 days report
           list of funds with proposed allocations
           allocation type - fixed/percent
           detect pattern - if 1-3 expenses in category - fixed sum
        */
    }

    private async Task AlterBudgetProposal()
    {
        // TODO
    }

    private async Task CreateBudget()
    {
        // TODO
    }

    private async Task CreateBudgetAllocations()
    {
        // TODO
    }

    private async Task FetchAllBudgets()
    {
        // TODO
    }

    private async Task FetchAllLedgers()
    {
        // TODO
    }

    private async Task FetchAllTransactions()
    {
        // TODO
    }

    private async Task FetchAllAccounts()
    {
        // TODO
    }

    private async Task CreateTransferBetweenLedgers()
    {
        // TODO
    }

    private async Task CreateNewBudgetsAllocations()
    {
        // TODO
    }

    private async Task CreateBudgetsForNewLedgers()
    {
        // TODO
    }

    private async Task AssignExisitngAccountsToNewLedgers()
    {
        // TODO
    }

    private async Task CreateNewLedgers()
    {
        // TODO
    }

    private async Task UpdateAccount()
    {
        // TODO
    }

    private async Task CreateNewAccount()
    {
        // TODO
    }

    private async Task DeleteAccount()
    {
        // TODO
    }

    private async Task OpenPlannedBudget()
    {
        // TODO
    }

    private async Task CloseActiveBudget()
    {
        // TODO
    }

    private async Task UpdatePlannedBudget()
    {
        // TODO
    }

    private async Task CreatePlannedBudget()
    {
        // TODO
    }

    private async Task UpdateBudgetTransactions()
    {
        // TODO
    }

    private async Task FetchBudgetTransactionLog()
    {
        // TODO
    }

    private async Task CreateLedgerTransactionsLinkedToBudgetTransactions()
    {
        // TODO
    }

    private async Task CreateBasicBudgetTransactions()
    {
        // TODO
    }

    private async Task FetchBudgetSummary()
    {
        // TODO
    }

    private async Task Logout()
    {
        // TODO
    }
#pragma warning restore CS1998, CA1822
}

public class PriorityOrderer : ITestCaseOrderer
{
    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(
        IEnumerable<TTestCase> testCases) where TTestCase : ITestCase
    {
        var assemblyName = typeof(TestPriorityAttribute).AssemblyQualifiedName!;
        var sortedMethods = new SortedDictionary<int, List<TTestCase>>();

        foreach (TTestCase testCase in testCases)
        {
            var priority = testCase.TestMethod.Method
                .GetCustomAttributes(assemblyName)
                .FirstOrDefault()
                ?.GetNamedArgument<int>(nameof(TestPriorityAttribute.Priority)) ?? 0;

            if (!sortedMethods.ContainsKey(priority))
                sortedMethods[priority] = [];

            sortedMethods[priority].Add(testCase);
        }

        foreach (var list in sortedMethods.Values)
            foreach (var testCase in list)
                yield return testCase;
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class TestPriorityAttribute(int priority) : Attribute
{
    public int Priority { get; } = priority; // XUnit doesn't preserve declaration order
}
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class SequentialCollection { }


public class TestState
{
    public string UserEmail = "test@test.test";
    public string UserName = "Test User";
    public string UserPassword = "1234";
    public string Token = "invalid";

    public Guid? UserId { get; set; }
    public Guid? LedgerId { get; set; }
    public CreateLedgerCommand? CreatedLedger { get; set; }
    public LedgerDTO? Ledger { get; set; }
    public LedgerTransactionsDTO? LedgerTransactions { get; set; }

    public CreateAccountTransactionCommand[]? CreatedIncomes { get; set; }
    public CreateAccountTransactionCommand[]? CreatedExpenses { get; set; }
    public CreateAccountTransferCommand[]? CreatedTransfers { get; set; }
    public CreateCurrencyExchangeCommand[]? CreatedCurrencyExchanges { get; set; }
}