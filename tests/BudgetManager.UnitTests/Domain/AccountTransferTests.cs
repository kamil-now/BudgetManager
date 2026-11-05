using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Models;
using Shouldly;

namespace BudgetManager.UnitTests.Domain;

public class AccountTransferTests
{
    private static readonly Guid SourceAccountId = Guid.NewGuid();
    private static readonly Guid TargetAccountId = Guid.NewGuid();

    [Fact]
    public void Transfer_ShouldCreateOppositeTransactions()
    {
        // Act
        var transfer = AccountTransfer.Transfer(SourceAccountId, TargetAccountId, new(100, "PLN"), DateTimeOffset.UtcNow, "title", "comment", ["tag"]);

        // Assert
        transfer.ShouldSatisfyAllConditions(
            x => x.Expense.AccountId.ShouldBe(SourceAccountId),
            x => x.Expense.Value.ShouldBe(new Money(-100, "PLN")),
            x => x.Expense.Type.ShouldBe(AccountTransactionType.TransferOut),
            x => x.Income.AccountId.ShouldBe(TargetAccountId),
            x => x.Income.Value.ShouldBe(new Money(100, "PLN")),
            x => x.Income.Type.ShouldBe(AccountTransactionType.TransferIn),
            x => x.Income.Tags.ShouldBe(["tag"]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Transfer_WhenValueIsNotPositive_ShouldThrow(decimal amount)
    {
        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            AccountTransfer.Transfer(SourceAccountId, TargetAccountId, new(amount, "PLN"), DateTimeOffset.UtcNow, null, null, null));
    }

    [Fact]
    public void Transfer_WhenAccountsAreTheSame_ShouldThrow()
    {
        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            AccountTransfer.Transfer(SourceAccountId, SourceAccountId, new(100, "PLN"), DateTimeOffset.UtcNow, null, null, null));
    }

    [Fact]
    public void Exchange_ShouldNegateSell()
    {
        // Act
        var transfer = AccountTransfer.Exchange(SourceAccountId, SourceAccountId, new(100, "USD"), new(90, "EUR"), DateTimeOffset.UtcNow, null, null, null);

        // Assert
        transfer.ShouldSatisfyAllConditions(
            x => x.Expense.Value.ShouldBe(new Money(-100, "USD")),
            x => x.Income.Value.ShouldBe(new Money(90, "EUR")));
    }

    [Fact]
    public void Exchange_WhenCurrenciesAreTheSame_ShouldThrow()
    {
        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            AccountTransfer.Exchange(SourceAccountId, TargetAccountId, new(100, "PLN"), new(90, "PLN"), DateTimeOffset.UtcNow, null, null, null));
    }

    [Fact]
    public void UpdateTransfer_ShouldUpdateBothTransactions()
    {
        // Arrange
        var transfer = AccountTransfer.Transfer(SourceAccountId, TargetAccountId, new(100, "PLN"), DateTimeOffset.UtcNow, null, null, null);
        var date = DateTimeOffset.UtcNow.AddDays(-1);

        // Act
        transfer.UpdateTransfer(TargetAccountId, SourceAccountId, new(50, "EUR"), date, "title", "comment", ["tag"]);

        // Assert
        transfer.ShouldSatisfyAllConditions(
            x => x.Expense.AccountId.ShouldBe(TargetAccountId),
            x => x.Expense.Value.ShouldBe(new Money(-50, "EUR")),
            x => x.Expense.Date.ShouldBe(date),
            x => x.Income.AccountId.ShouldBe(SourceAccountId),
            x => x.Income.Value.ShouldBe(new Money(50, "EUR")),
            x => x.Income.Date.ShouldBe(date),
            x => x.Income.Title.ShouldBe("title"));
    }

    [Fact]
    public void Update_WhenInitialBalanceMovesToAnotherAccount_ShouldThrow()
    {
        // Arrange
        var transaction = AccountTransaction.CreateInitialBalance(SourceAccountId, new(100, "PLN"));

        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            transaction.Update(TargetAccountId, new(100, "PLN"), DateTimeOffset.UtcNow, null, null, null));
    }

    [Fact]
    public void Update_WhenTransactionIsTransfer_ShouldThrow()
    {
        // Arrange
        var transfer = AccountTransfer.Transfer(SourceAccountId, TargetAccountId, new(100, "PLN"), DateTimeOffset.UtcNow, null, null, null);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            transfer.Income.Update(TargetAccountId, new(500, "PLN"), DateTimeOffset.UtcNow, null, null, null));
    }
}
