using BudgetManager.Application.Commands;
using BudgetManager.Application.Models;
using BudgetManager.Application.Queries;
using BudgetManager.Common;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;
using BudgetManager.Domain.Models;
using Xunit.Abstractions;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace BudgetManager.IntegrationTests.Application;

public abstract class BaseTest(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : TestBed<ApplicationFixture>(testOutputHelper, fixture)
{
    protected IEntityStore EntityStore => _fixture.GetService<IEntityStore>(_testOutputHelper)
      ?? throw new InvalidOperationException($"{nameof(IEntityStore)} is not registered in the service collection.");

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

    /// <summary>
    /// A ledger of the current user with a "Cash" account (empty initial balance) and a "Budget" with the given funds.
    /// </summary>
    protected async Task<LedgerDTO> SeedLedgerAsync(params string[] fundNames)
    {
        var ledgerId = await Mediator.Send(new CreateLedgerCommand(
            $"Ledger {Guid.NewGuid()}",
            [new CreateBudgetDTO("Budget", [.. (fundNames.Length == 0 ? ["Food", "Rent"] : fundNames).Select(x => new CreateFundDTO(x))])],
            [new CreateAccountDTO(new(0, "EUR"), "Cash")]));

        return await Mediator.Send(new GetLedgerQuery(ledgerId));
    }

    protected async Task<decimal> UnallocatedAsync(Guid ledgerId, string currency = "EUR")
    {
        var ledger = await Mediator.Send(new GetLedgerQuery(ledgerId));
        return ledger.Balance.GetValueOrDefault(currency)
            - ledger.Budgets.SelectMany(x => x.Funds).Sum(x => x.Balance.GetValueOrDefault(currency));
    }

    protected async Task<decimal> FundBalanceAsync(Guid ledgerId, Guid fundId, string currency = "EUR")
        => (await Mediator.Send(new GetLedgerQuery(ledgerId))).Budgets.SelectMany(x => x.Funds).Single(x => x.Id == fundId).Balance.GetValueOrDefault(currency);

    protected Task<Guid> CreateTemplateAsync(Guid budgetId, string name, string currency, params AllocationLine[] lines)
        => Mediator.Send(new CreateAllocationTemplateCommand(budgetId, name, currency, lines));

    protected static Guid FundId(LedgerDTO ledger, string name)
        => ledger.Budgets.SelectMany(x => x.Funds).Single(x => x.Name == name).Id;
}
