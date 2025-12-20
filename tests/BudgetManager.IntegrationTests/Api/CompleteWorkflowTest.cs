using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BudgetManager.Api.Models;
using BudgetManager.Application.Commands;
using BudgetManager.Application.Models;
using BudgetManager.Common.Enums;
using BudgetManager.Common.Models;
using BudgetManager.Domain.Entities;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Api;

public class CompleteWorkflowTest(ITestOutputHelper testOutputHelper, ApiFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    private const string _userEmail = "test@test.test";
    private const string _userName = "Test User";
    private const string _userPassword = "1234";

    private string _token = "invalid";

    [Fact]
    public async Task CompleteWorkflow_Works()
    {
        await Register();
        await Login();

        var (id, expectedLedger) = await CreateLedgerWithAccounts();
        var ledger = await FetchLedgerSummary(id, expectedLedger);

        await CreateAccountTransactions([.. ledger.Accounts]);
        await FetchTransactionTags();
        await FetchLedgerTransactionLog();
        await UpdateTransactions();

        await Logout();
        await Login();

        await FetchSpendingReports();
        await GenerateBudgetProposal();
        await AlterBudgetProposal();
        await CreateBudget();

        await CreateBudgetAllocations();
        await FetchBudgetSummary();
        await CreateBasicBudgetTransactions();
        await CreateLedgerTransactionsLinkedToBudgetTransactions();

        await FetchBudgetTransactionLog();
        await UpdateBudgetTransactions();

        await CreatePlannedBudget();
        await UpdatePlannedBudget();

        await CloseActiveBudget();
        await OpenPlannedBudget();

        await DeleteAccount();
        await CreateNewAccount();
        await UpdateAccount();

        await CreateNewLedgers();
        await AssignExisitngAccountsToNewLedgers();
        await CreateBudgetsForNewLedgers();
        await CreateNewBudgetsAllocations();

        await CreateTransferBetweenLedgers();

        await FetchAllAccounts();
        await FetchAllTransactions();
        await FetchAllLedgers();
        await FetchAllBudgets();
    }

    private async Task<Guid> Register()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register", new CreateUserCommand(_userEmail, _userPassword, _userName));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var id = await response.Content.ReadFromJsonAsync<Guid>();

        id.ShouldNotBe(Guid.Empty);

        return id;
    }

    private async Task Login()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new LoginCommand(_userEmail, _userPassword));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();

        tokenResponse.ShouldNotBeNull();

        _token = tokenResponse.Token;

        _token.ShouldNotBeEmpty();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
    }

    private async Task<(Guid id, CreateLedgerCommand command)> CreateLedgerWithAccounts()
    {
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
        return (id, command);
    }

    private async Task<LedgerDTO> FetchLedgerSummary(Guid id, CreateLedgerCommand expected)
    {
        var response = await Client.GetAsync($"/api/ledgers/{id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var ledger = await response.Content.ReadFromJsonAsync<LedgerDTO>();

        ledger.ShouldNotBeNull();

        ledger.Name.ShouldBe(expected.Name);
        ledger.Description.ShouldBe(expected.Description);

        ledger.Budgets.Count().ShouldBe(1);
        var budget = ledger.Budgets.First();

        budget.Name.ShouldBe(expected.Budget.Name);
        budget.Description.ShouldBe(expected.Budget.Description);

        budget.Funds.Count().ShouldBe(expected.Budget.Funds.Count());

        foreach (var expectedFund in expected.Budget.Funds)
        {
            var fund = budget.Funds.FirstOrDefault(x => x.Name == expectedFund.Name);
            fund.ShouldNotBeNull($"Fund with name '{expectedFund.Name}' not found.");
            fund.Description.ShouldBe(expectedFund.Description);
            fund.Balance.Keys.ShouldBeEmpty();
        }

        ledger.Accounts.Count().ShouldBe(expected.Accounts.Count());

        foreach (var expectedAccount in expected.Accounts)
        {
            var account = ledger.Accounts.FirstOrDefault(x => x.Name == expectedAccount.Name);
            account.ShouldNotBeNull($"Account with name '{expectedAccount.Name}' not found.");
            account.Description.ShouldBe(expectedAccount.Description);
            account.Balance.Keys.Count.ShouldBe(1);
            account.Balance.ContainsKey(expectedAccount.InitialBalance.Currency).ShouldBeTrue();
            account.Balance[expectedAccount.InitialBalance.Currency].ShouldBe(expectedAccount.InitialBalance.Amount);
        }

        return ledger;
    }

    private async Task CreateAccountTransactions(LedgerDTO.Account[] accounts)
    {
        accounts.Length.ShouldBe(3);

        async Task POST<T>(string url, T payload)
        {
            var response = await Client.PostAsJsonAsync(url, payload);
            response.StatusCode.ShouldBe(HttpStatusCode.OK, response.Content.ReadAsStringAsync().Result);
            var id = await response.Content.ReadFromJsonAsync<Guid>();
            id.ShouldNotBe(Guid.Empty);
        }

        var cashGift = new CreateIncomeCommand(accounts[0].Id, new Money(100, "USD"), DateTimeOffset.UtcNow.AddDays(-2), "gift", "birthday", ["extra"]);
        var currencyExchangeOut = new CreateExpenseCommand(accounts[0].Id, new Money(100, "USD"), DateTimeOffset.UtcNow.AddDays(-1), null, null, ["currency exchange"]);
        var currencyExchangeIn = new CreateIncomeCommand(accounts[0].Id, new Money(85.38m, "EUR"), DateTimeOffset.UtcNow.AddDays(-1), null, null, ["currency exchange"]);

        var salary = new CreateIncomeCommand(accounts[1].Id, new Money(3000.01234567890123456789m, "EUR"), DateTimeOffset.UtcNow, "Salary", null, ["regular"]);

        var savingsTransfer = new CreateTransferCommand(accounts[1].Id, accounts[2].Id, new Money(1000, "EUR"), DateTimeOffset.UtcNow, "monthly", "from salary", null);
        var rent = new CreateExpenseCommand(accounts[1].Id, new Money(1000, "EUR"), DateTimeOffset.UtcNow, "Rent", null, ["regular"]);

        await POST($"/api/accounts/{accounts[0].Id}/income", cashGift);
        await POST($"/api/accounts/{accounts[0].Id}/expense", currencyExchangeOut);
        await POST($"/api/accounts/{accounts[0].Id}/income", currencyExchangeIn);

        await POST($"/api/accounts/{accounts[1].Id}/income", salary);
        await POST($"/api/accounts/{accounts[1].Id}/transfer", savingsTransfer);
        await POST($"/api/accounts/{accounts[1].Id}/expense", rent);
    }

#pragma warning disable CS1998, CA1822

    private async Task FetchLedgerTransactionLog()
    {
        // TODO
    }

    private async Task UpdateTransactions()
    {
        // TODO
    }

    private async Task Logout()
    {
        // TODO
    }

    private async Task FetchSpendingReports()
    {
        // TODO
    }

    private async Task GenerateBudgetProposal()
    {
        // TODO
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

    private async Task FetchTransactionTags()
    {
        // TODO
    }
#pragma warning restore CS1998, CA1822
}
