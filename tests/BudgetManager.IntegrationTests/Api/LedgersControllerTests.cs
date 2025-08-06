using System.Net;
using System.Net.Http.Json;
using BudgetManager.Application.Commands;
using BudgetManager.Common.Enums;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Api;

public class LedgersControllerTests(ITestOutputHelper testOutputHelper, ApiFixture fixture) : BaseTest(testOutputHelper, fixture)
{
  private const string _baseUrl = "/api/ledgers";

  [Fact]
  public async Task CreateLedger_WhenUserIsUnauthorized_Fails()
  => await AssertPostFailsWhenUnauthorized(_baseUrl);

  [Fact]
  public async Task CreateLedger_WhenBearerTokenIsInvalid_Fails()
  => await AssertFailsWhenTokenIsInvalid(_baseUrl);

  [Fact]
  public async Task CreateLedger_WhenBearerTokenIsValid_Succeeds()
  {
    // Arrange
    await RegisterAndLogin();
    var command = new CreateLedgerCommand(
      "[ledger name]",
      "[ledger description]",
      new("[budget name]", [new("[fund name]", 42, 20, AllocationType.Percent, "[fund description]")], "[budget description]"),
      [new(new(123, "EUR"), "[account name]", "[account description]")]); ;

    // Act
    var response = await Client.PostAsJsonAsync(_baseUrl, command);

    // Assert
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
  }

  [Fact]
  public async Task CreateLedger_WhenFailsValidation_BadRequest()
  {
    // Arrange
    await RegisterAndLogin();

    // Act
    var response = await Client.PostAsJsonAsync(_baseUrl, new { });

    // Assert
    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
  }
  // TODO
}