using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Models;
using Shouldly;

namespace BudgetManager.UnitTests.Domain;

public class AllocationTests
{
    private static readonly Guid LedgerId = Guid.NewGuid();
    private static readonly Guid Food = Guid.NewGuid();
    private static readonly Guid Rent = Guid.NewGuid();
    private static readonly DateTimeOffset Date = new(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WhenAmountsAreGiven_ShouldCreateRowsStampedWithDate()
    {
        // Act
        var allocation = Allocation.Create(LedgerId, "EUR", Date, [new FundAmount(Food, 30), new FundAmount(Rent, 70)]);

        // Assert
        allocation.ShouldSatisfyAllConditions(
            x => x.LedgerId.ShouldBe(LedgerId),
            x => x.Currency.ShouldBe("EUR"),
            x => x.IsInitial.ShouldBeFalse(),
            x => x.Total.ShouldBe(100),
            x => x.Transactions.Select(t => (t.FundId, t.Value, t.Date, t.AllocationId)).ShouldBe([
                (Food, new Money(30, "EUR"), Date, (Guid?)allocation.Id),
                (Rent, new Money(70, "EUR"), Date, (Guid?)allocation.Id)]));
    }

    [Fact]
    public void CreateInitial_WhenCalled_ShouldCreateInitialAllocationWithDefaultDate()
    {
        // Act
        var allocation = Allocation.CreateInitial(LedgerId, "EUR", [new FundAmount(Food, 30)]);

        // Assert
        allocation.ShouldSatisfyAllConditions(
            x => x.IsInitial.ShouldBeTrue(),
            x => x.Date.ShouldBe(default),
            x => x.Transactions.ShouldHaveSingleItem().Date.ShouldBe(default));
    }

    [Fact]
    public void Update_WhenAllocationIsInitial_And_DateChanges_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var allocation = Allocation.CreateInitial(LedgerId, "EUR", [new FundAmount(Food, 30)]);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => allocation.Update("EUR", Date));
    }

    [Fact]
    public void Update_WhenAllocationIsInitial_And_DateStays_ShouldStayInitial()
    {
        // Arrange
        var allocation = Allocation.CreateInitial(LedgerId, "EUR", [new FundAmount(Food, 30)]);

        // Act
        allocation.Update("PLN", default);

        // Assert
        allocation.ShouldSatisfyAllConditions(
            x => x.IsInitial.ShouldBeTrue(),
            x => x.Currency.ShouldBe("PLN"));
    }

    [Fact]
    public void Update_WhenCalled_ShouldRestampRows()
    {
        // Arrange
        var allocation = Allocation.Create(LedgerId, "EUR", Date, [new FundAmount(Food, 30), new FundAmount(Rent, 70)]);
        var newDate = Date.AddDays(5);

        // Act
        allocation.Update("PLN", newDate);

        // Assert
        allocation.ShouldSatisfyAllConditions(
            x => x.Currency.ShouldBe("PLN"),
            x => x.Date.ShouldBe(newDate),
            x => x.Transactions.ShouldAllBe(t => t.Date == newDate));
    }
}
