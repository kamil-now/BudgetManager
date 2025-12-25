using BudgetManager.Common;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;
using Xunit.Abstractions;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace BudgetManager.IntegrationTests.Application;

public abstract class BaseTest(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : TestBed<ApplicationFixture>(testOutputHelper, fixture)
{
  protected IBudgetManagerService BudgetManagerService => _fixture.GetService<IBudgetManagerService>(_testOutputHelper)
    ?? throw new InvalidOperationException($"{nameof(IBudgetManagerService)} is not registered in the service collection.");

  protected IMediator Mediator => _fixture.GetService<IMediator>(_testOutputHelper)
    ?? throw new InvalidOperationException($"{nameof(IMediator)} is not registered in the service collection.");

  protected MockCurrentUserService MockCurrentUserService => _fixture.GetService<MockCurrentUserService>(_testOutputHelper)
    ?? throw new InvalidOperationException($"{nameof(MockCurrentUserService)} is not registered in the service collection.");

  protected async Task<Guid> MockAuthenticatedUserAsync()
  {
    var userId = Guid.NewGuid();
    MockCurrentUserService.MockUserId = userId.ToString();
    var user = new User
    {
      Id = userId,
      Name = "Test User",
      Email = $"test{Guid.NewGuid()}@email.com",
      HashedPassword = "Test Hashed Password"
    };

    await BudgetManagerService.CreateAsync(user);
    await BudgetManagerService.SaveChangesAsync();
    return userId;
  }

  protected void MockUnauthenticatedUser()
  {
    MockCurrentUserService.MockUserId = null;
  }
}
