using BudgetManager.Domain;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Models;
using BudgetManager.Domain.Services;
using Shouldly;

namespace BudgetManager.UnitTests.Domain;

public class AllocationCalculatorTests
{
    private static readonly Guid FundA = Guid.NewGuid();
    private static readonly Guid FundB = Guid.NewGuid();
    private static readonly Guid FundC = Guid.NewGuid();

    [Fact]
    public void Allocate_WhenPercentStepIsFollowedByFixedStep_ShouldTakePercentFirstThenFixed()
    {
        // Arrange
        AllocationRule[] rules =
        [
            new(FundA, 0, AllocationType.Percent, 50),
            new(FundB, 1, AllocationType.Fixed, 200),
        ];

        // Act
        var result = AllocationCalculator.Allocate(1000, rules);

        // Assert
        result.ShouldBe([new FundAmount(FundA, 500), new FundAmount(FundB, 200)]);
    }

    [Fact]
    public void Allocate_WhenStepsMixFixedAndPercents_ShouldTakePercentOfWhatTheStepsBeforeLeft()
    {
        // Arrange
        AllocationRule[] rules =
        [
            new(FundA, 0, AllocationType.Fixed, 200),
            new(FundB, 1, AllocationType.Percent, 10),
            new(FundC, 2, AllocationType.Percent, 25),
        ];

        // Act
        var result = AllocationCalculator.Allocate(1000, rules);

        // Assert
        result.ShouldBe([new FundAmount(FundA, 200), new FundAmount(FundB, 80), new FundAmount(FundC, 180)]);
    }

    [Fact]
    public void Allocate_WhenRulesAreNotInOrder_ShouldRunThemBySequence()
    {
        // Arrange
        AllocationRule[] rules =
        [
            new(FundB, 1, AllocationType.Fixed, 200),
            new(FundA, 0, AllocationType.Percent, 50),
        ];

        // Act
        var result = AllocationCalculator.Allocate(1000, rules);

        // Assert
        result.ShouldBe([new FundAmount(FundA, 500), new FundAmount(FundB, 200)]);
    }

    [Fact]
    public void Allocate_WhenPercentsRoundDown_ShouldRoundEachStepTowardsZeroAndLetLastHundredPercentTakeRest()
    {
        // Arrange
        AllocationRule[] rules =
        [
            new(FundA, 0, AllocationType.Percent, 33.33m),
            new(FundB, 1, AllocationType.Percent, 33.33m),
            new(FundC, 2, AllocationType.Percent, 100),
        ];

        // Act
        var result = AllocationCalculator.Allocate(10, rules);

        // Assert
        result.ShouldBe([new FundAmount(FundA, 3.33m), new FundAmount(FundB, 2.22m), new FundAmount(FundC, 4.45m)]);
    }

    [Fact]
    public void Allocate_WhenLastStepIsHundredPercent_ShouldAllocateEverything()
    {
        // Arrange
        AllocationRule[] rules =
        [
            new(FundA, 0, AllocationType.Fixed, 30),
            new(FundB, 1, AllocationType.Percent, 100),
        ];

        // Act
        var result = AllocationCalculator.Allocate(100.01m, rules);

        // Assert
        result.Sum(x => x.Amount).ShouldBe(100.01m);
    }

    [Fact]
    public void Allocate_WhenStepsDoNotTakeEverything_ShouldLeaveLeftoverUnallocated()
    {
        // Arrange
        AllocationRule[] rules =
        [
            new(FundA, 0, AllocationType.Percent, 50),
            new(FundB, 1, AllocationType.Percent, 30),
        ];

        // Act
        var result = AllocationCalculator.Allocate(100.01m, rules);

        // Assert
        result.ShouldBe([new FundAmount(FundA, 50), new FundAmount(FundB, 15)]);
    }

    [Fact]
    public void Allocate_WhenOneCentIsSplitFiftyPercentThenRest_ShouldGiveItToLastStep()
    {
        // Arrange
        AllocationRule[] rules =
        [
            new(FundA, 0, AllocationType.Percent, 50),
            new(FundB, 1, AllocationType.Percent, 100),
        ];

        // Act
        var result = AllocationCalculator.Allocate(0.01m, rules);

        // Assert
        result.ShouldBe([new FundAmount(FundB, 0.01m)]);
    }

    [Fact]
    public void Allocate_WhenFundHasSeveralSteps_ShouldReturnAnAmountPerStep()
    {
        // Arrange
        AllocationRule[] rules =
        [
            new(FundA, 0, AllocationType.Fixed, 100),
            new(FundB, 1, AllocationType.Percent, 50),
            new(FundA, 2, AllocationType.Percent, 100),
        ];

        // Act
        var result = AllocationCalculator.Allocate(300, rules);

        // Assert
        result.ShouldBe([new FundAmount(FundA, 100), new FundAmount(FundB, 100), new FundAmount(FundA, 100)]);
    }

    [Fact]
    public void Allocate_WhenIncomeIsBelowMinimum_ShouldThrow()
    {
        // Arrange
        AllocationRule[] rules =
        [
            new(FundA, 0, AllocationType.Fixed, 100),
            new(FundB, 1, AllocationType.Fixed, 50),
        ];

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => AllocationCalculator.Allocate(149.99m, rules));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Allocate_WhenIncomeIsNotPositive_ShouldReturnNothing(decimal income)
    {
        // Arrange
        AllocationRule[] rules = [new(FundA, 0, AllocationType.Fixed, 100)];

        // Act
        var result = AllocationCalculator.Allocate(income, rules);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void Minimum_WhenStepsAreFixedOnly_ShouldBeTheirSum()
    {
        // Arrange
        AllocationRule[] rules =
        [
            new(FundA, 0, AllocationType.Fixed, 100),
            new(FundB, 1, AllocationType.Fixed, 50.5m),
        ];

        // Act
        var result = AllocationCalculator.Minimum(rules);

        // Assert
        result.ShouldBe(150.5m);
    }

    [Fact]
    public void Minimum_WhenStepsArePercentsOnly_ShouldBeZero()
    {
        // Arrange
        AllocationRule[] rules =
        [
            new(FundA, 0, AllocationType.Percent, 50),
            new(FundB, 1, AllocationType.Percent, 100),
        ];

        // Act
        var result = AllocationCalculator.Minimum(rules);

        // Assert
        result.ShouldBe(0);
    }

    [Fact]
    public void Minimum_WhenFixedStepFollowsFiftyPercent_ShouldBeOneCentBelowDoubleTheFixedAmount()
    {
        // Arrange
        // 199.99 * 50% rounds down to 99.99, which leaves exactly 100.00
        AllocationRule[] rules =
        [
            new(FundA, 0, AllocationType.Percent, 50),
            new(FundB, 1, AllocationType.Fixed, 100),
        ];

        // Act
        var result = AllocationCalculator.Minimum(rules);

        // Assert
        result.ShouldBe(199.99m);
    }

    [Fact]
    public void Minimum_WhenFixedStepFollowsRoundedPercent_ShouldBeTheSmallestAmountThatFundsIt()
    {
        // Arrange
        AllocationRule[] rules =
        [
            new(FundA, 0, AllocationType.Percent, 33.33m),
            new(FundB, 1, AllocationType.Fixed, 100),
        ];

        // Act
        var result = AllocationCalculator.Minimum(rules);

        // Assert
        result.ShouldBe(149.98m);
    }

    [Fact]
    public void Minimum_WhenPercentStepIsBetweenFixedSteps_ShouldFundBoth()
    {
        // Arrange
        AllocationRule[] rules =
        [
            new(FundA, 0, AllocationType.Fixed, 100),
            new(FundB, 1, AllocationType.Percent, 50),
            new(FundC, 2, AllocationType.Fixed, 100),
        ];

        // Act
        var result = AllocationCalculator.Minimum(rules);

        // Assert
        result.ShouldBe(299.99m);
    }

    [Fact]
    public void Minimum_WhenFixedStepFollowsHundredPercentStep_ShouldExceedMaxMoneyAmount()
    {
        // Arrange
        AllocationRule[] rules =
        [
            new(FundA, 0, AllocationType.Percent, 100),
            new(FundB, 1, AllocationType.Fixed, 1),
        ];

        // Act
        var result = AllocationCalculator.Minimum(rules);

        // Assert
        result.ShouldBeGreaterThan(Constants.MaxMoneyAmount);
    }

    [Theory]
    [InlineData(33.33, 100)]
    [InlineData(50, 100)]
    [InlineData(12.5, 77.77)]
    public void Allocate_WhenIncomeIsTheMinimum_ShouldFundEveryFixedStep(double percent, double fixedAmount)
    {
        // Arrange
        AllocationRule[] rules =
        [
            new(FundA, 0, AllocationType.Percent, (decimal)percent),
            new(FundB, 1, AllocationType.Fixed, (decimal)fixedAmount),
        ];
        var minimum = AllocationCalculator.Minimum(rules);

        // Act
        var result = AllocationCalculator.Allocate(minimum, rules);

        // Assert
        result.Single(x => x.FundId == FundB).Amount.ShouldBe((decimal)fixedAmount);
    }

    [Fact]
    public void Allocate_WhenIncomeIsOneCentBelowMinimum_ShouldThrow()
    {
        // Arrange
        AllocationRule[] rules =
        [
            new(FundA, 0, AllocationType.Percent, 33.33m),
            new(FundB, 1, AllocationType.Fixed, 100),
        ];
        var minimum = AllocationCalculator.Minimum(rules);

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => AllocationCalculator.Allocate(minimum - 0.01m, rules));
    }
}
