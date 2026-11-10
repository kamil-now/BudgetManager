using BudgetManager.Application.Handlers;
using BudgetManager.Application.Interfaces;
using BudgetManager.Application.Queries;
using BudgetManager.Application.Validators;
using NSubstitute;
using Shouldly;

namespace BudgetManager.UnitTests.Application;

public class GetLedgerTests
{
    [Fact]
    public async Task GetLedger_WhenLedgerDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        var ledgerReader = Substitute.For<ILedgerReader>();

        var handler = new GetLedgerHandler(ledgerReader);

        // Act & Assert
        await Should.ThrowAsync<NotFoundException>(() => handler.Handle(new GetLedgerQuery(Guid.Empty), default));
    }
}
