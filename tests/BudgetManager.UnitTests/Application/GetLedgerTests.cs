using BudgetManager.Application.Handlers;
using BudgetManager.Application.Interfaces;
using BudgetManager.Application.Queries;
using NSubstitute;
using Shouldly;

namespace BudgetManager.UnitTests.Application;

public class GetLedgerTests
{
    [Fact]
    public async Task GetLedger_WhenLedgerDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var ledgerReader = Substitute.For<ILedgerReader>();

        var handler = new GetLedgerHandler(ledgerReader);

        // Act
        var ledger = await handler.Handle(new GetLedgerQuery(Guid.Empty), default);

        // Assert
        ledger.ShouldBeNull();
    }
}
