using BudgetManager.Application.Commands;
using BudgetManager.Domain.Entities;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Application;

public class CreateAccountTests(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task CreateAccount_WhenUserDoesNotExist_ThrowsException()
    {
        // Arrange
        var command = new CreateAccountCommand(Guid.NewGuid(), null, new(0, "PLN"), "Test Account", "Test Account Description");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => Mediator.Send(command));
    }

    [Fact]
    public async Task CreateAccount_WhenLedgerDoesNotExist_ThrowsException()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = $"test{Guid.NewGuid()}@email.com",
            HashedPassword = "Test Hashed Password"
        };
        await BudgetManagerService.CreateAsync(user);
        await BudgetManagerService.SaveChangesAsync();

        var command = new CreateAccountCommand(user.Id, Guid.NewGuid(), new(0, "PLN"), "Test Account", "Test Account Description");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => Mediator.Send(command));
    }

    [Fact]
    public async Task CreateAccount_CreatesNewEntity()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = $"test{Guid.NewGuid()}@email.com",
            HashedPassword = "Test Hashed Password"
        };
        await BudgetManagerService.CreateAsync(user);
        await BudgetManagerService.SaveChangesAsync();

        var command = new CreateAccountCommand(user.Id, null, new(123, "PLN"), "Test Account", "Test Account Description");

        // Act
        var accountId = await Mediator.Send(command);

        // Assert
        var account = await BudgetManagerService.GetAsync<Account>(accountId);

        account.ShouldNotBeNull();
        account.Id.ShouldBe(accountId);
        account.Name.ShouldBe(command.Name);
        account.Description.ShouldBe(command.Description);
        account.OwnerId.ShouldBe(user.Id);
        account.LedgerId.ShouldBeNull();
        // account.CurrentBalance.ShouldBe(new Money(0, "PLN"));
    }
}
