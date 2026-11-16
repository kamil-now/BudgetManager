using System.Net;
using System.Net.Http.Json;
using BudgetManager.Application.Commands;
using BudgetManager.Domain;
using BudgetManager.Domain.Enums;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Api.Ledgers;

public class LedgersControllerTests(ITestOutputHelper testOutputHelper, ApiFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    private const string _baseUrl = "/api/ledgers";

    internal static CreateLedgerCommand ValidCommand = new(
        "[ledger name]",
        [
          new(
            "[budget name]",
            [
              new("[fund name]")
            ],
            [
              new("[template name]", "EUR", [new("[fund name]", 42, AllocationType.Percent, Percent: 20)])
            ])
        ],
        [
          new(new(123, "EUR"), "[account name]")
        ],
        [
          new("[budget name]", "[fund name]", new(123, "EUR"))
        ]);

    [Fact]
    public async Task CreateLedger_WhenUserIsUnauthorized_401()
    => await AssertPostFailsWhenUnauthorized(_baseUrl);

    [Fact]
    public async Task CreateLedger_WhenBearerTokenIsInvalid_401()
    => await AssertUnauthorizedWhenTokenIsInvalid(_baseUrl);

    [Fact]
    public async Task CreateLedger_WhenRequestIsValid_OK()
    {
        // Arrange
        await RegisterAndLogin();

        // Act
        var response = await Client.PostAsJsonAsync(_baseUrl, ValidCommand);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetLedger_WhenLedgerBelongsToAnotherUser_403()
    {
        // Arrange
        await RegisterAndLogin();
        var response = await Client.PostAsJsonAsync(_baseUrl, ValidCommand);
        var ledgerId = await response.Content.ReadFromJsonAsync<Guid>();

        await RegisterAndLogin();

        // Act
        var foreignResponse = await Client.GetAsync($"{_baseUrl}/{ledgerId}");

        // Assert
        foreignResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateLedger_WhenRequestIsInvalid_400()
    {
        // Arrange
        await RegisterAndLogin();

        // Act
        var response = await Client.PostAsJsonAsync(_baseUrl, new { });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    public sealed class InvalidRequests : TheoryData<CreateLedgerCommand, string>
    {
        public InvalidRequests()
        {
            var tooLongName = new string('b', Constants.MaxNameLength + 1);
            var validAccount = ValidCommand.Accounts.First();
            var validBudget = ValidCommand.Budgets.First();
            var validFund = validBudget.Funds.First();
            var validTemplate = validBudget.AllocationTemplates!.First();
            var validLine = validTemplate.Lines.First();

            Add(ValidCommand with { Name = string.Empty }, "Ledger name cannot be empty.");

            Add(ValidCommand with { Name = tooLongName }, $"Ledger name value is too long. Max length is {Constants.MaxNameLength}.");

            Add(ValidCommand with { Accounts = [null!] }, "Account cannot be empty.");

            Add(ValidCommand with { Budgets = [null!] }, "Budget cannot be empty.");

            Add(ValidCommand with { Budgets = [validBudget with { Funds = [null!] }] }, "Fund cannot be empty.");

            Add(ValidCommand with { Accounts = [] }, "Accounts cannot be empty.");

            Add(ValidCommand with { Accounts = [validAccount with { Name = string.Empty }] }, "Account name cannot be empty.");

            Add(ValidCommand with { Accounts = [validAccount with { Name = tooLongName }] }, $"Account name value is too long. Max length is {Constants.MaxNameLength}.");

            Add(ValidCommand with { Accounts = [validAccount with { InitialBalance = new(1, "USDD") }] }, $"InitialBalance of {validAccount.Name} currency value 'USDD' is not a valid currency code.");

            Add(ValidCommand with { Accounts = [validAccount with { InitialBalance = new(1, "USD1") }] }, $"InitialBalance of {validAccount.Name} currency value 'USD1' is not a valid currency code.");

            Add(ValidCommand with { Accounts = [validAccount with { InitialBalance = new(1.234m, "USD") }] }, $"InitialBalance of {validAccount.Name} amount value '1.234' cannot have more than {Constants.MoneyDecimalPlaces} decimal places.");

            Add(ValidCommand with { Accounts = [validAccount, validAccount] }, "Account names must be unique.");

            Add(ValidCommand with { Budgets = [] }, "Budgets cannot be empty.");

            Add(ValidCommand with { Budgets = [validBudget, validBudget] }, "Budget names must be unique.");

            Add(ValidCommand with { Budgets = [validBudget, validBudget with { Name = string.Empty }] }, "Budget name cannot be empty.");

            Add(ValidCommand with { Budgets = [validBudget with { Name = string.Empty }] }, "Budget name cannot be empty.");

            Add(ValidCommand with { Budgets = [validBudget with { Name = tooLongName }] }, $"Budget name value is too long. Max length is {Constants.MaxNameLength}.");

            Add(ValidCommand with { Budgets = [validBudget with { Funds = [] }] }, "Funds cannot be empty.");

            Add(ValidCommand with { Budgets = [validBudget with { Funds = [validFund with { Name = string.Empty }] }] }, "Fund name cannot be empty.");

            Add(ValidCommand with { Budgets = [validBudget with { Funds = [validFund with { Name = tooLongName }] }] }, $"Fund name value is too long. Max length is {Constants.MaxNameLength}.");

            Add(ValidCommand with { Budgets = [validBudget with { AllocationTemplates = [validTemplate with { Lines = [validLine with { Sequence = -1 }] }] }] }, $"Allocation template {validTemplate.Name} sequence must be greater than or equal zero.");

            Add(ValidCommand with { Budgets = [validBudget with { AllocationTemplates = [validTemplate with { Lines = [validLine with { Percent = 100.01m }] }] }] }, $"Allocation template {validTemplate.Name} percent must be less than or equal {Constants.MaxAllocationPercent}.");

            Add(ValidCommand with { Budgets = [validBudget with { AllocationTemplates = [validTemplate with { Lines = [validLine with { Percent = 0 }] }] }] }, $"Allocation template {validTemplate.Name} percent must be greater than zero.");

            Add(ValidCommand with { Budgets = [validBudget with { AllocationTemplates = [validTemplate, validTemplate] }] }, "Allocation template names in a budget must be unique.");

            Add(ValidCommand with { Budgets = [validBudget with { AllocationTemplates = [validTemplate with { Lines = [validLine with { Fund = "[missing]" }] }] }] }, $"Allocation template {validTemplate.Name} allocates to unknown fund [missing].");

            Add(ValidCommand with { Budgets = [validBudget with { Funds = [validFund, validFund] }] }, "Fund names in a budget must be unique.");

            Add(ValidCommand with { InitialAllocations = null }, "Initial allocations in EUR must add up to 123 (got 0).");

            Add(ValidCommand with { InitialAllocations = [new("[budget name]", "[fund name]", new(100, "EUR"))] }, "Initial allocations in EUR must add up to 123 (got 100).");
        }
    }

    [Theory]
    [ClassData(typeof(InvalidRequests))]
    public async Task CreateLedger_WhenCommandFailsValidation_422_WithErrorMessage(CreateLedgerCommand command, string expectedError)
    {
        // Arrange
        await RegisterAndLogin();

        // Act
        var response = await Client.PostAsJsonAsync(_baseUrl, command);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableContent);

        var error = await response.Content.ReadAsStringAsync();
        error.ShouldBeEquivalentTo(expectedError);
    }
}
