using BudgetManager.Application.Commands;
using BudgetManager.Application.Models;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Application;

public class CreateBudgetTests(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    private static readonly CreateFundDTO[] _funds = [new("Test Fund", 0, 100, AllocationType.Fixed, "Test Fund Description")];

    [Fact]
    public async Task CreateBudget_WhenUserIsNotAuthenticated_ShouldThrowException()
    {
        // Arrange
        MockUnauthenticatedUser();
        var command = new CreateBudgetCommand(Guid.NewGuid(), "Test Budget", _funds);

        // Act & Assert
        await Should.ThrowAsync<AuthenticationException>(() => Mediator.Send(command));
    }

    [Fact]
    public async Task CreateBudget_WhenLedgerDoesNotExist_ShouldThrowException()
    {
        // Arrange
        await MockAuthenticatedUserAsync();
        var command = new CreateBudgetCommand(Guid.NewGuid(), "Test Budget", _funds);

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() => Mediator.Send(command));
    }

    [Fact]
    public async Task CreateBudget_WhenLedgerBelongsToAnotherUser_ShouldThrowException()
    {
        // Arrange
        var otherUserId = await MockAuthenticatedUserAsync();
        var ledger = await CreateLedgerAsync(otherUserId);

        await MockAuthenticatedUserAsync();
        var command = new CreateBudgetCommand(ledger.Id, "Test Budget", _funds);

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() => Mediator.Send(command));
    }

    [Fact]
    public async Task CreateBudget_WhenNameIsEmpty_ShouldThrowException()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var ledger = await CreateLedgerAsync(userId);

        var command = new CreateBudgetCommand(ledger.Id, string.Empty, _funds);

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe("Budget name cannot be empty.");
    }

    [Fact]
    public async Task CreateBudget_WhenNameIsTooLong_ShouldThrowException()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var ledger = await CreateLedgerAsync(userId);

        var command = new CreateBudgetCommand(ledger.Id, new string('a', Constants.MaxNameLength + 1), _funds);

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe($"Budget name value is too long. Max length is {Constants.MaxNameLength}.");
    }

    [Fact]
    public async Task CreateBudget_WhenFundsAreEmpty_ShouldThrowException()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var ledger = await CreateLedgerAsync(userId);

        var command = new CreateBudgetCommand(ledger.Id, "Test Budget", []);

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe("Funds cannot be empty.");
    }

    [Fact]
    public async Task CreateBudget_WhenFundAllocationSequenceIsNegative_ShouldThrowException()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var ledger = await CreateLedgerAsync(userId);

        var command = new CreateBudgetCommand(ledger.Id, "Test Budget", [new("Test Fund", -1, 100, AllocationType.Fixed)]);

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe("AllocationTemplateSequence of Test Fund must be greater than or equal zero.");
    }

    [Fact]
    public async Task CreateBudget_WhenBudgetWithSameNameExistsInLedger_ShouldThrowException()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var ledger = await CreateLedgerAsync(userId);
        var command = new CreateBudgetCommand(ledger.Id, "Test Budget", _funds);
        await Mediator.Send(command);

        // Act & Assert
        var ex = await Should.ThrowAsync<ConflictException>(() => Mediator.Send(command));
        ex.Message.ShouldBe($"Budget with name {command.Name} already exists in ledger {ledger.Id}.");
    }

    [Fact]
    public async Task CreateBudget_WhenBudgetWithSameNameExistsInAnotherLedger_ShouldCreateNewEntity()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var ledger = await CreateLedgerAsync(userId);
        var otherLedger = await CreateLedgerAsync(userId);
        await Mediator.Send(new CreateBudgetCommand(otherLedger.Id, "Test Budget", _funds));

        // Act
        var budgetId = await Mediator.Send(new CreateBudgetCommand(ledger.Id, "Test Budget", _funds));

        // Assert
        budgetId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateBudget_ShouldCreateNewEntity()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var ledger = await CreateLedgerAsync(userId);

        var command = new CreateBudgetCommand(ledger.Id, "Test Budget", _funds, "Test Budget Description");

        // Act
        var budgetId = await Mediator.Send(command);

        // Assert
        var budget = await EntityStore.GetAsync<Budget>(budgetId);

        budget.ShouldNotBeNull();
        budget.Id.ShouldBe(budgetId);
        budget.Name.ShouldBe(command.Name);
        budget.Description.ShouldBe(command.Description);
        budget.LedgerId.ShouldBe(ledger.Id);
    }

    [Fact]
    public async Task CreateBudget_ShouldCreateFundsFromTemplate()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var ledger = await CreateLedgerAsync(userId);

        var command = new CreateBudgetCommand(ledger.Id, "Test Budget", _funds);

        // Act
        var budgetId = await Mediator.Send(command);

        // Assert
        var funds = (await EntityStore.GetAsync<Fund>(x => x.BudgetId == budgetId)).ToArray();

        funds.Length.ShouldBe(1);
        funds[0].ShouldSatisfyAllConditions(
            fund => fund.Name.ShouldBe(_funds[0].Name),
            fund => fund.Description.ShouldBe(_funds[0].Description),
            fund => fund.AllocationTemplateSequence.ShouldBe(_funds[0].AllocationTemplateSequence),
            fund => fund.AllocationTemplateValue.ShouldBe(_funds[0].AllocationTemplateValue),
            fund => fund.AllocationTemplateType.ShouldBe(_funds[0].AllocationTemplateType));
    }

    private async Task<Ledger> CreateLedgerAsync(Guid ownerId)
    {
        var ledger = await EntityStore.CreateAsync(new Ledger { OwnerId = ownerId, Name = $"Ledger {Guid.NewGuid()}" });
        await EntityStore.SaveChangesAsync();
        return ledger;
    }
}
