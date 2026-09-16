using BudgetManager.Application.Queries;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Entities;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Application;

public class GetLedgerTests(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task GetLedger_WhenLedgerBelongsToAnotherUser_ShouldThrowAuthorizationException()
    {
        // Arrange
        var ledgerId = await CreateLedgerOfAnotherUserAsync();

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() => Mediator.Send(new GetLedgerQuery(ledgerId)));
    }

    [Fact]
    public async Task GetLedgerStatistics_WhenLedgerBelongsToAnotherUser_ShouldThrowAuthorizationException()
    {
        // Arrange
        var ledgerId = await CreateLedgerOfAnotherUserAsync();

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() => Mediator.Send(new GetLedgerStatisticsQuery(ledgerId, new())));
    }

    [Fact]
    public async Task GetLedger_WhenLedgerBelongsToTheUser_ShouldReturnLedger()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var ledger = await EntityStore.CreateAsync(new Ledger { OwnerId = userId, Name = "Own Ledger" });
        await EntityStore.SaveChangesAsync();

        // Act
        var result = await Mediator.Send(new GetLedgerQuery(ledger.Id));

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(ledger.Id);
    }

    private async Task<Guid> CreateLedgerOfAnotherUserAsync()
    {
        var otherUserId = await MockAuthenticatedUserAsync();
        var ledger = await EntityStore.CreateAsync(new Ledger { OwnerId = otherUserId, Name = "Other User Ledger" });
        await EntityStore.SaveChangesAsync();

        await MockAuthenticatedUserAsync();

        return ledger.Id;
    }
}
