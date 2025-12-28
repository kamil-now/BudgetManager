using System.Net;
using System.Net.Http.Json;
using BudgetManager.Application.Commands;
using BudgetManager.Common.Enums;
using BudgetManager.Domain;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Api.Ledgers;

public class LedgersControllerTests(ITestOutputHelper testOutputHelper, ApiFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    private const string _baseUrl = "/api/ledgers";

    internal static CreateLedgerCommand ValidCommand = new(
        "[ledger name]",
        "[ledger description]",
        new(
          "[budget name]",
          [
            new("[fund name]", 42, 20, AllocationType.Percent, "[fund description]")
          ],
          "[budget description]"),
        [
          new(new(123, "EUR"), "[account name]", "[account description]")
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
            var tooLongDescription = new string('a', Constants.MaxCommentLength + 1);
            var tooLongName = new string('b', Constants.MaxNameLength + 1);
            var validAccount = ValidCommand.Accounts.First();
            var validFund = ValidCommand.Budget.Funds.First();

            Add(ValidCommand with { Description = tooLongDescription }, $"Description value is too long. Max length is {Constants.MaxCommentLength}.");

            Add(ValidCommand with { Accounts = [] }, "Accounts cannot be empty.");

            Add(ValidCommand with { Accounts = [validAccount with { Name = string.Empty }] }, "Account name cannot be empty.");

            Add(ValidCommand with { Accounts = [validAccount with { Name = tooLongName }] }, $"Account name value is too long. Max length is {Constants.MaxNameLength}.");

            Add(ValidCommand with { Accounts = [validAccount with { Description = tooLongDescription }] }, $"Description of {validAccount.Name} value is too long. Max length is {Constants.MaxCommentLength}.");

            Add(ValidCommand with { Accounts = [validAccount with { InitialBalance = new(-1, "USD") }] }, $"InitialBalance of {validAccount.Name} amount must be greater than zero.");

            Add(ValidCommand with { Accounts = [validAccount with { InitialBalance = new(1, "USDD") }] }, $"InitialBalance of {validAccount.Name} currency value 'USDD' is not a valid currency code.");

            Add(ValidCommand with { Accounts = [validAccount with { InitialBalance = new(1, "USD1") }] }, $"InitialBalance of {validAccount.Name} currency value 'USD1' is not a valid currency code.");

            Add(ValidCommand with { Budget = ValidCommand.Budget with { Name = string.Empty } }, "Budget name cannot be empty.");

            Add(ValidCommand with { Budget = ValidCommand.Budget with { Name = tooLongName } }, $"Budget name value is too long. Max length is {Constants.MaxNameLength}.");

            Add(ValidCommand with { Budget = ValidCommand.Budget with { Description = tooLongDescription } }, $"Description of {ValidCommand.Budget.Name} value is too long. Max length is {Constants.MaxCommentLength}.");

            Add(ValidCommand with { Budget = ValidCommand.Budget with { Funds = [] } }, "Funds cannot be empty.");

            Add(ValidCommand with { Budget = ValidCommand.Budget with { Funds = [validFund with { Name = string.Empty }] } }, "Fund name cannot be empty.");

            Add(ValidCommand with { Budget = ValidCommand.Budget with { Funds = [validFund with { Name = tooLongName }] } }, $"Fund name value is too long. Max length is {Constants.MaxNameLength}.");

            Add(ValidCommand with { Budget = ValidCommand.Budget with { Funds = [validFund with { Description = tooLongDescription }] } }, $"Description of {validFund.Name} value is too long. Max length is {Constants.MaxCommentLength}.");

            Add(ValidCommand with { Budget = ValidCommand.Budget with { Funds = [validFund with { AllocationTemplateSequence = -1 }] } }, $"AllocationTemplateSequence of {validFund.Name} must be greater than or equal zero.");
        }
    }

    [Theory]
    [ClassData(typeof(InvalidRequests))]
    public async Task CreateLedger_WhenCommandFailsValidation_400_WithErrorMessage(CreateLedgerCommand command, string expectedError)
    {
        // Arrange
        await RegisterAndLogin();

        // Act
        var response = await Client.PostAsJsonAsync(_baseUrl, command);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var error = await response.Content.ReadAsStringAsync();
        error.ShouldBeEquivalentTo(expectedError);
    }
}
