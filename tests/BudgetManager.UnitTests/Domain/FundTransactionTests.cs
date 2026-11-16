using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Models;
using Shouldly;

namespace BudgetManager.UnitTests.Domain;

public class FundTransactionTests
{
    private static readonly Guid FundId = Guid.NewGuid();
    private static readonly DateTimeOffset Date = new(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Allocate_WhenSourceIsIncome_ShouldTakeDateAndTitleOfIncomeAndLinkIt()
    {
        // Arrange
        var income = AccountTransaction.Create(Guid.NewGuid(), new Money(100, "EUR"), Date, "Salary");

        // Act
        var transaction = FundTransaction.Allocate(FundId, new Money(40, "EUR"), income);

        // Assert
        transaction.ShouldSatisfyAllConditions(
            x => x.FundId.ShouldBe(FundId),
            x => x.Value.ShouldBe(new Money(40, "EUR")),
            x => x.Date.ShouldBe(Date),
            x => x.Title.ShouldBe("Salary"),
            x => x.AccountTransactionId.ShouldBe(income.Id),
            x => x.AllocationId.ShouldBeNull());
    }

    [Fact]
    public void Allocate_WhenSourceIsAllocation_ShouldTakeDateOfAllocationAndLinkIt()
    {
        // Arrange
        var allocation = Allocation.Create(Guid.NewGuid(), "EUR", Date, []);

        // Act
        var transaction = FundTransaction.Allocate(FundId, new Money(40, "EUR"), allocation);

        // Assert
        transaction.ShouldSatisfyAllConditions(
            x => x.FundId.ShouldBe(FundId),
            x => x.Value.ShouldBe(new Money(40, "EUR")),
            x => x.Date.ShouldBe(Date),
            x => x.AllocationId.ShouldBe(allocation.Id),
            x => x.AccountTransactionId.ShouldBeNull());
    }

    [Fact]
    public void Pay_WhenExpenseIsGiven_ShouldCreateNegativeRowLinkedToIt()
    {
        // Arrange
        var expense = AccountTransaction.Create(Guid.NewGuid(), new Money(-25, "EUR"), Date, "Groceries");

        // Act
        var transaction = FundTransaction.Pay(FundId, expense);

        // Assert
        transaction.ShouldSatisfyAllConditions(
            x => x.FundId.ShouldBe(FundId),
            x => x.Value.ShouldBe(new Money(-25, "EUR")),
            x => x.Date.ShouldBe(Date),
            x => x.Title.ShouldBe("Groceries"),
            x => x.AccountTransactionId.ShouldBe(expense.Id),
            x => x.AllocationId.ShouldBeNull());
    }

    [Fact]
    public void Pay_WhenTransactionIsIncome_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var income = AccountTransaction.Create(Guid.NewGuid(), new Money(25, "EUR"), Date, "Salary");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => FundTransaction.Pay(FundId, income));
    }
}
