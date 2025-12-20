using BudgetManager.Application.Handlers;
using BudgetManager.Application.Queries;
using BudgetManager.Application.Services;
using BudgetManager.Domain.Interfaces;
using NSubstitute;
using Shouldly;

namespace BudgetManager.UnitTests.Application;

public class GetLedgerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("not a guid")]
    public async Task GetLedger_WhenUserIdIsInvalid_ThenThrowsException(string? currentUserId)
    {
        // Arrange
        var budgetService = Substitute.For<IBudgetManagerService>();
        var currentUserService = Substitute.For<ICurrentUserService>();

        currentUserService.Id.Returns(currentUserId);

        var handler = new GetLedgerHandler(currentUserService, budgetService);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await handler.Handle(new GetLedgerQuery(), default));

        // Assert
        exception.Message.ShouldBe($"Current user ID value is invalid '{currentUserId}'.");
    }

    [Fact]
    public async Task GetLedger_WhenLedgerDoesNotExist_ReturnsNull()
    {
        // Arrange
        var budgetService = Substitute.For<IBudgetManagerService>();
        var currentUserService = Substitute.For<ICurrentUserService>();

        currentUserService.Id.Returns(Guid.NewGuid().ToString());

        var handler = new GetLedgerHandler(currentUserService, budgetService);

        // Act
        var ledger = await handler.Handle(new GetLedgerQuery(), default);

        // Assert
        ledger.ShouldBeNull();
    }
}
