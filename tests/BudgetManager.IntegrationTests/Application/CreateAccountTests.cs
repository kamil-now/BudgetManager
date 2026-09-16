using BudgetManager.Application.Commands;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Models;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Application;

public class CreateAccountTests(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task CreateAccount_WhenUserDoesNotExist_ShouldThrowException()
    {
        // Arrange
        MockUnauthenticatedUser();
        var command = new CreateAccountCommand(Guid.NewGuid(), new(0, "PLN"), "Test Account", "Test Account Description");

        // Act & Assert
        var ex = await Should.ThrowAsync<AuthenticationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe("User ID '00000000-0000-0000-0000-000000000000' is invalid.");
    }

    [Fact]
    public async Task CreateAccount_WhenLedgerDoesNotExist_ShouldThrowException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();

        var command = new CreateAccountCommand(Guid.NewGuid(), new(0, "PLN"), "Test Account", "Test Account Description");

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() => Mediator.Send(command));
    }

    [Fact]
    public async Task CreateAccount_WhenLedgerBelongsToAnotherUser_ShouldThrowException()
    {
        // Arrange
        var otherUserId = await MockAuthenticatedUserAsync();
        var ledger = await CreateLedgerAsync(otherUserId);

        await MockAuthenticatedUserAsync();
        var command = new CreateAccountCommand(ledger.Id, new(0, "PLN"), "Test Account", "Test Account Description");

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() => Mediator.Send(command));
    }

    [Fact]
    public async Task CreateAccount_WhenInitialBalanceHasTooManyDecimalPlaces_ShouldThrowException()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var ledger = await CreateLedgerAsync(userId);

        var command = new CreateAccountCommand(ledger.Id, new(123.456m, "PLN"), "Test Account", "Test Account Description");

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe($"InitialBalance amount value '123.456' cannot have more than {Constants.MoneyDecimalPlaces} decimal places.");
    }

    [Fact]
    public async Task CreateAccount_ShouldCreateNewEntity()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var ledger = await CreateLedgerAsync(userId);

        var command = new CreateAccountCommand(ledger.Id, new(123, "PLN"), "Test Account", "Test Account Description");

        // Act
        var accountId = await Mediator.Send(command);

        // Assert
        var account = await EntityStore.GetAsync<Account>(accountId);

        account.ShouldNotBeNull();
        account.Id.ShouldBe(accountId);
        account.Name.ShouldBe(command.Name);
        account.Description.ShouldBe(command.Description);
        account.LedgerId.ShouldBe(ledger.Id);
        var transactions = (await EntityStore.GetAsync<AccountTransaction>(x => x.AccountId == accountId)).ToArray();
        transactions.Length.ShouldBe(1);
        transactions.First().Value.ShouldBe(command.InitialBalance);
    }

    private async Task<Ledger> CreateLedgerAsync(Guid ownerId)
    {
        var ledger = await EntityStore.CreateAsync(new Ledger { OwnerId = ownerId, Name = $"Ledger {Guid.NewGuid()}" });
        await EntityStore.SaveChangesAsync();
        return ledger;
    }
}
