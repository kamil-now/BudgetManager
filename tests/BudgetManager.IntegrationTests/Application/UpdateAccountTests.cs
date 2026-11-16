using BudgetManager.Application.Commands;
using BudgetManager.Application.Queries;
using BudgetManager.Application.Validators;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Application;

public class UpdateAccountTests(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task UpdateAccount_WhenValid_ShouldRenameIt()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync();
        var accountId = ledger.Accounts.Single().Id;

        // Act
        await Mediator.Send(new UpdateAccountCommand(accountId, "Bank"));

        // Assert
        (await Mediator.Send(new GetAccountQuery(accountId))).Name.ShouldBe("Bank");
    }

    [Fact]
    public async Task UpdateAccount_WhenNameIsUnchanged_ShouldNotConflictWithItself()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync();

        // Act & Assert
        await Should.NotThrowAsync(() => Mediator.Send(new UpdateAccountCommand(ledger.Accounts.Single().Id, "Cash")));
    }

    [Fact]
    public async Task UpdateAccount_WhenNameIsTakenByAnotherAccount_ShouldThrowConflictException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync();
        var otherId = await Mediator.Send(new CreateAccountCommand(ledger.Id, new(0, "EUR"), "Bank"));

        // Act & Assert
        await Should.ThrowAsync<ConflictException>(() => Mediator.Send(new UpdateAccountCommand(otherId, "Cash")));
    }

    [Fact]
    public async Task UpdateAccount_WhenNameIsTakenInAnotherLedger_ShouldSucceed()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync();
        var otherLedger = await SeedLedgerAsync();
        var otherId = await Mediator.Send(new CreateAccountCommand(otherLedger.Id, new(0, "EUR"), "Bank"));

        // Act & Assert
        await Should.NotThrowAsync(() => Mediator.Send(new UpdateAccountCommand(ledger.Accounts.Single().Id, "Bank")));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task UpdateAccount_WhenNameIsEmpty_ShouldThrowValidationException(string? name)
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync();

        // Act & Assert
        await Should.ThrowAsync<ValidationException>(() => Mediator.Send(new UpdateAccountCommand(ledger.Accounts.Single().Id, name!)));
    }

    [Fact]
    public async Task UpdateAccount_WhenAccountBelongsToAnotherUser_ShouldThrowAuthorizationException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var ledger = await SeedLedgerAsync();

        await MockAuthenticatedUserAsync();

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() => Mediator.Send(new UpdateAccountCommand(ledger.Accounts.Single().Id, "Bank")));
    }
}
