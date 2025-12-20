using BudgetManager.Application.Handlers;
using BudgetManager.Application.Queries;
using BudgetManager.Domain.Interfaces;
using NSubstitute;
using Shouldly;

namespace BudgetManager.UnitTests.Application;

public class GetLedgerTests
{
    [Fact]
    public async Task GetLedger_WhenLedgerDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var budgetService = Substitute.For<IBudgetManagerService>();

        var handler = new GetLedgerHandler(budgetService);

        // Act
        var ledger = await handler.Handle(new GetLedgerQuery(Guid.Empty), default);

        // Assert
        ledger.ShouldBeNull();
    }
}
