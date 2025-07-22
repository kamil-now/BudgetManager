using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BudgetManager.Api.Models;
using BudgetManager.Application.Commands;
using BudgetManager.Common.Enums;
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

    await CreateLedgerWithAccounts();
    await FetchLedgerSummary();
    await CreateLedgerTransactions();
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

  private async Task Register()
  {
    var response = await Client.PostAsJsonAsync("/api/auth/register", new CreateUserCommand(_userEmail, _userPassword, _userName));

    response.StatusCode.ShouldBe(HttpStatusCode.OK);

    var id = await response.Content.ReadAsStringAsync();

    id.ShouldNotBeEmpty();
    id.ShouldNotBe(Guid.Empty.ToString());
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

  private async Task CreateLedgerWithAccounts()
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

    var id = await response.Content.ReadAsStringAsync();

    id.ShouldNotBeEmpty();
    id.ShouldNotBe(Guid.Empty.ToString());
  }

#pragma warning disable CS1998, CA1822

  private async Task FetchLedgerSummary()
  {
    // TODO
  }

  private async Task CreateLedgerTransactions()
  {
    // TODO
  }

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
