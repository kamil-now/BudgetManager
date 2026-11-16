using BudgetManager.Application.Commands;
using BudgetManager.Application.Models;
using BudgetManager.Application.Queries;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Application;

public class DeleteUserTests(ITestOutputHelper testOutputHelper, ApplicationFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    private const string Password = "password";

    private ApplicationDbContext DbContext => _fixture.GetService<ApplicationDbContext>(_testOutputHelper)
        ?? throw new InvalidOperationException($"{nameof(ApplicationDbContext)} is not registered in the service collection.");

    [Fact]
    public async Task DeleteUser_WhenPasswordIsCorrect_ShouldDeleteUser()
    {
        // Arrange
        var userId = await CreateUserAsync();

        // Act
        await Mediator.Send(new DeleteUserCommand(Password));

        // Assert
        (await DbContext.Users.AsNoTracking().AnyAsync(x => x.Id == userId)).ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteUser_WhenUserHasLedgers_ShouldDeleteEverythingTheyOwn()
    {
        // Arrange
        var userId = await CreateUserAsync();
        var seed = await SeedRichLedgerAsync();

        // Act
        await Mediator.Send(new DeleteUserCommand(Password));

        // Assert
        var db = DbContext;
        (await db.Users.AsNoTracking().AnyAsync(x => x.Id == userId)).ShouldBeFalse();
        (await db.Ledgers.AsNoTracking().AnyAsync(x => x.OwnerId == userId)).ShouldBeFalse();
        (await db.Accounts.AsNoTracking().AnyAsync(x => x.LedgerId == seed.LedgerId)).ShouldBeFalse();
        (await db.AccountTransactions.AsNoTracking().AnyAsync(x => seed.AccountIds.Contains(x.AccountId))).ShouldBeFalse();
        (await db.AccountTransfers.AsNoTracking().AnyAsync(x => seed.AccountIds.Contains(x.Income.AccountId) || seed.AccountIds.Contains(x.Expense.AccountId))).ShouldBeFalse();
        (await db.Budgets.AsNoTracking().AnyAsync(x => x.LedgerId == seed.LedgerId)).ShouldBeFalse();
        (await db.Funds.AsNoTracking().AnyAsync(x => seed.FundIds.Contains(x.Id))).ShouldBeFalse();
        (await db.FundTransactions.AsNoTracking().AnyAsync(x => seed.FundIds.Contains(x.FundId))).ShouldBeFalse();
        (await db.Allocations.AsNoTracking().AnyAsync(x => x.LedgerId == seed.LedgerId)).ShouldBeFalse();
        (await db.AllocationTemplates.AsNoTracking().AnyAsync(x => x.Budget.LedgerId == seed.LedgerId)).ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteUser_WhenOtherUsersHaveLedgers_ShouldKeepThem()
    {
        // Arrange
        await CreateUserAsync();
        var kept = await SeedRichLedgerAsync();
        var keptUserId = MockCurrentUserService.MockUserId;
        var deletedUserId = await CreateUserAsync();
        await SeedRichLedgerAsync();

        // Act
        await Mediator.Send(new DeleteUserCommand(Password));

        // Assert
        MockCurrentUserService.MockUserId = keptUserId;
        var ledger = await Mediator.Send(new GetLedgerQuery(kept.LedgerId));
        ledger.Accounts.Count().ShouldBe(2);
        (await DbContext.Users.AsNoTracking().AnyAsync(x => x.Id == deletedUserId)).ShouldBeFalse();
        (await DbContext.FundTransactions.AsNoTracking().CountAsync(x => kept.FundIds.Contains(x.FundId))).ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task DeleteUser_WhenPasswordIsWrong_ShouldThrowValidationException()
    {
        // Arrange
        var userId = await CreateUserAsync();

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(new DeleteUserCommand("wrong password")));
        ex.Message.ShouldBe("Password is incorrect.");
        (await DbContext.Users.AsNoTracking().AnyAsync(x => x.Id == userId)).ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task DeleteUser_WhenPasswordIsEmpty_ShouldThrowValidationException(string? password)
    {
        // Arrange
        var userId = await CreateUserAsync();

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => Mediator.Send(new DeleteUserCommand(password!)));
        ex.Message.ShouldBe("Password cannot be empty.");
        (await DbContext.Users.AsNoTracking().AnyAsync(x => x.Id == userId)).ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteUser_WhenUserIsNotAuthenticated_ShouldThrowAuthenticationException()
    {
        // Arrange
        MockUnauthenticatedUser();

        // Act & Assert
        await Should.ThrowAsync<AuthenticationException>(() => Mediator.Send(new DeleteUserCommand(Password)));
    }

    [Fact]
    public async Task DeleteUser_WhenUserWasDeleted_ShouldRejectFurtherRequests()
    {
        // Arrange
        await CreateUserAsync();
        await Mediator.Send(new DeleteUserCommand(Password));

        // Act & Assert
        await Should.ThrowAsync<AuthenticationException>(() => Mediator.Send(new GetLedgersQuery()));
    }

    private async Task<Guid> CreateUserAsync()
    {
        var user = await Mediator.Send(new CreateUserCommand($"test{Guid.NewGuid()}@email.com", Password, "Test User"));
        MockCurrentUserService.MockUserId = user.Id.ToString();
        return user.Id;
    }

    private sealed record RichLedger(Guid LedgerId, Guid[] AccountIds, Guid[] FundIds);

    /// <summary>
    /// A ledger of the current user with every kind of data: transfer, exchange, allocated income, allocation, expense paid from a fund, initial allocation and a template.
    /// </summary>
    private async Task<RichLedger> SeedRichLedgerAsync()
    {
        var ledgerId = await Mediator.Send(new CreateLedgerCommand(
            $"Ledger {Guid.NewGuid()}",
            [new CreateBudgetDTO("Budget", [new CreateFundDTO("Food"), new CreateFundDTO("Rent")],
                [new CreateAllocationTemplateDTO("Template", "EUR", [new("Food", 0, AllocationType.Fixed, Amount: 10)])])],
            [new CreateAccountDTO(new(100, "EUR"), "Cash"), new CreateAccountDTO(new(0, "EUR"), "Bank")],
            [new InitialAllocationDTO("Budget", "Food", new(100, "EUR"))]));
        var ledger = await Mediator.Send(new GetLedgerQuery(ledgerId));
        var cash = ledger.Accounts.Single(x => x.Name == "Cash").Id;
        var bank = ledger.Accounts.Single(x => x.Name == "Bank").Id;
        var food = FundId(ledger, "Food");
        var rent = FundId(ledger, "Rent");
        var date = DateTimeOffset.UtcNow.AddDays(-1);

        await Mediator.Send(new CreateAccountTransferCommand(cash, bank, new(20, "EUR"), date, "Transfer"));
        await Mediator.Send(new CreateCurrencyExchangeCommand(cash, bank, new(11, "USD"), new(10, "EUR"), date, "Exchange"));
        await Mediator.Send(new CreateAccountTransactionCommand(cash, new(100, "EUR"), date, "Salary",
            Allocation: new(Lines: [new(food, 0, AllocationType.Fixed, 30)])));
        await Mediator.Send(new AllocateFundsCommand(ledgerId, "EUR", new(Lines: [new(rent, 0, AllocationType.Fixed, 20)]), Amount: 20));
        await Mediator.Send(new CreateAccountTransactionCommand(cash, new(-10, "EUR"), date, "Lunch", FundId: food));

        return new RichLedger(ledgerId, [cash, bank], [food, rent]);
    }
}
