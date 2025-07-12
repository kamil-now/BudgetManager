using BudgetManager.Common;
using BudgetManager.Domain.Interfaces;
using BudgetManager.IntegrationTests.Fixtures;
using Xunit.Abstractions;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace BudgetManager.IntegrationTests.Application;

public abstract class BaseTest(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : TestBed<ApplicationFixture>(testOutputHelper, fixture)
{
  protected IBudgetManagerService BudgetManagerService => _fixture.GetService<IBudgetManagerService>(_testOutputHelper)
    ?? throw new InvalidOperationException($"{nameof(IBudgetManagerService)} is not registered in the service collection.");
  
  protected IMediator Mediator => _fixture.GetService<IMediator>(_testOutputHelper)
    ?? throw new InvalidOperationException($"{nameof(IMediator)} is not registered in the service collection.");
}
