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
    private static readonly CreateFundDTO[] _funds = [new("Test Fund", [new("EUR", 0, AllocationType.Fixed, Amount: 100)], "Test Fund Description")];

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

        var command = new CreateBudgetCommand(ledger.Id, "Test Budget", [new("Test Fund", [new("EUR", -1, AllocationType.Fixed, Amount: 100)])]);

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe("Allocation template of Test Fund sequence must be greater than or equal zero.");
    }

    [Fact]
    public async Task CreateBudget_WhenFundsShareAllocationSequenceInCurrency_ShouldThrowException()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var ledger = await CreateLedgerAsync(userId);

        var command = new CreateBudgetCommand(ledger.Id, "Test Budget", [
            new("Fund A", [new("EUR", 0, AllocationType.Fixed, Amount: 100)]),
            new("Fund B", [new("EUR", 0, AllocationType.Percent, Percent: 10)])
        ]);

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(command));
        ex.Message.ShouldBe("Allocation sequences in EUR must be unique.");
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
            fund => fund.Description.ShouldBe(_funds[0].Description));
    }

    [Fact]
    public async Task CreateBudget_ShouldCreateOneAllocationTemplatePerCurrency()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var ledger = await CreateLedgerAsync(userId);

        var command = new CreateBudgetCommand(ledger.Id, "Test Budget", [
            new("Fund A", [new("EUR", 0, AllocationType.Fixed, Amount: 100), new("USD", 0, AllocationType.Percent, Percent: 50)]),
            new("Fund B", [new("EUR", 1, AllocationType.Percent, Percent: 25)])
        ]);

        // Act
        var budgetId = await Mediator.Send(command);

        // Assert
        var templates = await EntityStore.GetAsync<AllocationTemplate>(x => x.BudgetId == budgetId);
        templates.Select(x => x.Currency).ShouldBe(["EUR", "USD"], ignoreOrder: true);
    }

    [Fact]
    public async Task CreateBudget_ShouldCreateAllocationTemplateLines()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var ledger = await CreateLedgerAsync(userId);

        var command = new CreateBudgetCommand(ledger.Id, "Test Budget", [
            new("Fund A", [new("EUR", 0, AllocationType.Fixed, Amount: 100.50m)]),
            new("Fund B", [new("EUR", 1, AllocationType.Percent, Percent: 12.34m)])
        ]);

        // Act
        var budgetId = await Mediator.Send(command);

        // Assert
        var funds = (await EntityStore.GetAsync<Fund>(x => x.BudgetId == budgetId)).ToDictionary(x => x.Name, x => x.Id);
        var template = (await EntityStore.GetAsync<AllocationTemplate>(x => x.BudgetId == budgetId)).ShouldHaveSingleItem();
        var lines = (await EntityStore.GetAsync<AllocationTemplateLine>(x => x.AllocationTemplateId == template.Id)).ToDictionary(x => x.FundId);

        lines.Count.ShouldBe(2);
        lines[funds["Fund A"]].ShouldBeOfType<FixedAllocationTemplateLine>().ShouldSatisfyAllConditions(
            line => line.Sequence.ShouldBe(0),
            line => line.Amount.ShouldBe(100.50m));
        lines[funds["Fund B"]].ShouldBeOfType<PercentAllocationTemplateLine>().ShouldSatisfyAllConditions(
            line => line.Sequence.ShouldBe(1),
            line => line.Percent.ShouldBe(12.34m));
    }

    [Fact]
    public async Task CreateBudget_WhenFundHasNoAllocationTemplates_ShouldCreateFundWithoutLines()
    {
        // Arrange
        var userId = await MockAuthenticatedUserAsync();
        var ledger = await CreateLedgerAsync(userId);

        var command = new CreateBudgetCommand(ledger.Id, "Test Budget", [new("Test Fund", [])]);

        // Act
        var budgetId = await Mediator.Send(command);

        // Assert
        (await EntityStore.GetAsync<Fund>(x => x.BudgetId == budgetId)).ShouldHaveSingleItem();
        (await EntityStore.GetAsync<AllocationTemplate>(x => x.BudgetId == budgetId)).ShouldBeEmpty();
    }

    private async Task<Ledger> CreateLedgerAsync(Guid ownerId)
    {
        var ledger = await EntityStore.CreateAsync(new Ledger { OwnerId = ownerId, Name = $"Ledger {Guid.NewGuid()}" });
        await EntityStore.SaveChangesAsync();
        return ledger;
    }
}
