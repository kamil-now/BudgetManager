using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Models;
using Shouldly;

namespace BudgetManager.UnitTests.Domain;

public class AccountTransactionTests
{
    private static readonly DateTimeOffset Date = new(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Update_WhenTransactionIsInitialBalance_And_DateChanges_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var initial = AccountTransaction.CreateInitialBalance(accountId, new Money(100, "EUR"));

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => initial.Update(accountId, new Money(100, "EUR"), Date, "Initial balance"));
    }

    [Fact]
    public void Update_WhenTransactionIsInitialBalance_And_DateStays_ShouldUpdateAmount()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var initial = AccountTransaction.CreateInitialBalance(accountId, new Money(100, "EUR"));

        // Act
        initial.Update(accountId, new Money(150, "EUR"), default, "Initial balance");

        // Assert
        initial.Value.ShouldBe(new Money(150, "EUR"));
    }

    [Fact]
    public void Update_WhenTransactionIsRegular_And_DateChanges_ShouldUpdateDate()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var transaction = AccountTransaction.Create(accountId, new Money(100, "EUR"), default, "Salary");

        // Act
        transaction.Update(accountId, new Money(100, "EUR"), Date, "Salary");

        // Assert
        transaction.Date.ShouldBe(Date);
    }
}
