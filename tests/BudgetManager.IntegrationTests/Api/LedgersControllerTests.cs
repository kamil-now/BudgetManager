using System.Net;
using System.Net.Http.Json;
using BudgetManager.Application.Commands;
using BudgetManager.Application.Models;
using BudgetManager.Common.Enums;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Api;

public class LedgersControllerTests(ITestOutputHelper testOutputHelper, ApiFixture fixture) : BaseTest(testOutputHelper, fixture)
{
  private const string _baseUrl = "/api/ledgers";

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
  public async Task CreateLedger_WhenRequestIsInvalid_400()
  {
    // Arrange
    await RegisterAndLogin();

    // Act
    var response = await Client.PostAsJsonAsync(_baseUrl, new { });

    // Assert
    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
  }

  [Fact]
  public async Task CreateLedger_WhenCommandFailsValidation_400_WithErrorMessage()
  {
    // Arrange
    await RegisterAndLogin();

    // Act
    var response = await Client.PostAsJsonAsync(_baseUrl,
    new CreateLedgerCommand(string.Empty, null, new CreateBudgetDTO(string.Empty, []), []));

    // Assert
    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

    var error = await response.Content.ReadAsStringAsync();
    error.ShouldBeEquivalentTo("Accounts cannot be empty.");
  }
}