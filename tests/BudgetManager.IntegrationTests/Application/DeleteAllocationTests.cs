using BudgetManager.Application.Commands;
using BudgetManager.Application.Models;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Models;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Application;

public class DeleteAllocationTests(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task DeleteAllocation_WhenAllocationHasSeveralFunds_ShouldDeleteItWithAllItsRows()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food", "Rent");
        var allocation = await AllocationAsync(ledger.Id, (FundId(ledger, "Food"), 20), (FundId(ledger, "Rent"), 30));

        // Act
        await Mediator.Send(new DeleteAllocationCommand(ledger.Id, allocation));

        // Assert
        (await EntityStore.ExistsAsync<Allocation>(x => x.Id == allocation)).ShouldBeFalse();
        (await EntityStore.ExistsAsync<FundTransaction>(x => x.AllocationId == allocation)).ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteAllocation_WhenAllocationIsInitial_ShouldDeleteIt()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledgerId = await Mediator.Send(new CreateLedgerCommand(
            $"Ledger {Guid.NewGuid()}",
            [new CreateBudgetDTO("Budget", [new CreateFundDTO("Food")])],
            [new CreateAccountDTO(new(100, "EUR"), "Cash")],
            [new InitialAllocationDTO("Budget", "Food", new(100, "EUR"))]));
        var allocation = (await EntityStore.GetAsync<Allocation>(x => x.LedgerId == ledgerId)).Single();

        // Act
        await Mediator.Send(new DeleteAllocationCommand(ledgerId, allocation.Id));

        // Assert
        (await EntityStore.ExistsAsync<Allocation>(x => x.Id == allocation.Id)).ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteAllocation_WhenIdIsAFundTransactionMadeFromIncome_ShouldThrowAuthorizationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var template = await CreateTemplateAsync(ledger.Budgets.Single().Id, "Salary", "EUR", new AllocationLine(FundId(ledger, "Food"), 0, AllocationType.Percent, Percent: 100));
        var income = await Mediator.Send(new CreateAccountTransactionCommand(
            ledger.Accounts.Single().Id, new(100, "EUR"), DateTimeOffset.UtcNow, "Salary", Allocation: new AllocationInput(TemplateId: template)));
        var row = (await EntityStore.GetAsync<FundTransaction>(x => x.AccountTransactionId == income)).Single();

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() => Mediator.Send(new DeleteAllocationCommand(ledger.Id, row.Id)));
    }

    [Fact]
    public async Task DeleteAllocation_WhenAllocationBelongsToAnotherLedger_ShouldThrowNotFoundException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var otherLedger = await SeedLedgerAsync("Rent");
        var allocation = await AllocationAsync(otherLedger.Id, (FundId(otherLedger, "Rent"), 20));

        // Act & Assert
        await Should.ThrowAsync<NotFoundException>(() => Mediator.Send(new DeleteAllocationCommand(ledger.Id, allocation)));
    }

    [Fact]
    public async Task DeleteAllocation_WhenAllocationBelongsToAnotherUser_ShouldThrowAuthorizationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync("Food");
        var allocation = await AllocationAsync(ledger.Id, (FundId(ledger, "Food"), 20));

        await MockAuthenticatedUserAsync();

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() => Mediator.Send(new DeleteAllocationCommand(ledger.Id, allocation)));
    }

    private async Task<Guid> AllocationAsync(Guid ledgerId, params (Guid FundId, decimal Amount)[] lines)
    {
        var allocation = await EntityStore.CreateAsync(Allocation.Create(
            ledgerId,
            "EUR",
            DateTimeOffset.UtcNow,
            lines.Select(x => new FundAmount(x.FundId, x.Amount))));
        await EntityStore.SaveChangesAsync();
        return allocation.Id;
    }
}
