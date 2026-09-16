using BudgetManager.Application.Interfaces;
using BudgetManager.Common;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;
using Xunit.Abstractions;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace BudgetManager.IntegrationTests.Application;

public abstract class BaseTest(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : TestBed<ApplicationFixture>(testOutputHelper, fixture)
{
  protected IEntityStore EntityStore => _fixture.GetService<IEntityStore>(_testOutputHelper)
    ?? throw new InvalidOperationException($"{nameof(IEntityStore)} is not registered in the service collection.");

  protected ILedgerReader LedgerReader => _fixture.GetService<ILedgerReader>(_testOutputHelper)
    ?? throw new InvalidOperationException($"{nameof(ILedgerReader)} is not registered in the service collection.");

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

    await EntityStore.CreateAsync(user);
    await EntityStore.SaveChangesAsync();
    return userId;
  }

  protected void MockUnauthenticatedUser()
  {
    MockCurrentUserService.MockUserId = null;
  }
}
