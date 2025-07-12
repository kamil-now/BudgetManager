using System.Net;
using System.Net.Http.Json;
using BudgetManager.Application.Commands;
using BudgetManager.Domain.Entities;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Api;

public class AccountsControllerTests(ITestOutputHelper testOutputHelper, ApiFixture fixture) : BaseTest(testOutputHelper, fixture)
{
  [Fact]
  public async Task CreateAccount_WhenUserIsUnauthorized_Fails()
  {
    // Act
    var response = await Client.PostAsJsonAsync("/api/accounts", new { });

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task CreateAccount_WhenBearerTokenIsInvalid_Fails()
  {
    // Arrange
    Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid_token");

    // Act
    var response = await Client.PostAsJsonAsync("/api/accounts", new { });

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }


  [Fact]
  public async Task CreateAccount_WhenBearerTokenIsValid_Succeeds()
  {
    // Arrange

    Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GetValidToken());

    // Act
    var response = await Client.PostAsJsonAsync("/api/accounts", new { });

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }
  // [Fact]
  // public async Task CreateAccount_WhenValidRequest_ReturnsCreated()
  // {
  //   // Arrange
  //   var user = new User
  //   {
  //     Id = Guid.NewGuid(),
  //     Name = "Test User",
  //     Email = $"test{Guid.NewGuid()}@email.com",
  //     HashedPassword = "Test Hashed Password"
  //   };
  //   var dbContext = GetContext();
  //   dbContext.Users.Add(user);
  //   var ledger = new Ledger
  //   {
  //     Id = Guid.NewGuid(),
  //     OwnerId = user.Id,
  //     Name = "Test Ledger",
  //     Description = "Test Description"
  //   };
  //   dbContext.Ledgers.Add(ledger);
  //   await dbContext.SaveChangesAsync();

  //   var request = new CreateAccountCommand(
  //     user.Id,
  //     ledger.Id,
  //     new(100, "PLN"),
  //     "Test Account",
  //     "Test Account Description");

  //   // Act
  //   var response = await Client.PostAsJsonAsync("/api/accounts", request);

  //   // Assert
  //   Assert.Equal(HttpStatusCode.Created, response.StatusCode);
  //   var location = response.Headers.Location?.ToString();
  //   Assert.NotNull(location);
  //   Assert.Contains("/api/accounts/", location);
  // }

}