using BudgetManager.Application.Commands;
using BudgetManager.Application.Validators;
using BudgetManager.Common.Models;
using BudgetManager.Domain.Entities;
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
        var command = new CreateAccountCommand(null, new(0, "PLN"), "Test Account", "Test Account Description");

        // Act & Assert
        var ex = await Should.ThrowAsync<AuthenticationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe("User not authenticated.");
    }

    [Fact]
    public async Task CreateAccount_WhenLedgerDoesNotExist_ShouldThrowException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();

        var command = new CreateAccountCommand(Guid.NewGuid(), new(0, "PLN"), "Test Account", "Test Account Description");

        // Act & Assert
        await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
    }

    [Fact]
    public async Task CreateAccount_ShouldCreateNewEntity()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();

        var command = new CreateAccountCommand(null, new(123, "PLN"), "Test Account", "Test Account Description");

        // Act
        var accountId = await Mediator.Send(command);

        // Assert
        var account = await BudgetManagerService.GetAsync<Account>(accountId);

        account.ShouldNotBeNull();
        account.Id.ShouldBe(accountId);
        account.Name.ShouldBe(command.Name);
        account.Description.ShouldBe(command.Description);
        account.OwnerId.ShouldBe(userId);
        account.LedgerId.ShouldBeNull();
        account.GetBalance().ShouldBeEquivalentTo(new Balance() { { command.InitialBalance.Currency, command.InitialBalance.Amount } });
    }
}
