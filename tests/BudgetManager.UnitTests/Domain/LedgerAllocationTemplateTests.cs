using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Models;
using Shouldly;

namespace BudgetManager.UnitTests.Domain;

public class LedgerAllocationTemplateTests
{
    private static readonly Guid BudgetA = Guid.NewGuid();
    private static readonly Guid BudgetB = Guid.NewGuid();

    [Fact]
    public void Create_ShouldAddALinePerGivenLine()
    {
        // Act
        var template = NewTemplate(
            new LedgerAllocationLine(BudgetA, 0, AllocationType.Fixed, Amount: 100),
            new LedgerAllocationLine(BudgetB, 1, AllocationType.Percent, Percent: 50));

        // Assert
        template.ToRules().ShouldBe([
            new AllocationRule(BudgetA, 0, AllocationType.Fixed, 100),
            new AllocationRule(BudgetB, 1, AllocationType.Percent, 50)]);
        template.Lines.ShouldAllBe(x => x.LedgerAllocationTemplateId == template.Id);
    }

    [Fact]
    public void ReplaceLines_ShouldReplaceEveryLineAndReturnTheNewOnes()
    {
        // Arrange
        var template = NewTemplate(new LedgerAllocationLine(BudgetA, 0, AllocationType.Fixed, Amount: 100));
        var line = template.Lines.Single();

        // Act
        var added = template.ReplaceLines([new LedgerAllocationLine(BudgetB, 0, AllocationType.Percent, Percent: 20)]);

        // Assert
        template.Lines.ShouldHaveSingleItem().ShouldNotBeSameAs(line);
        added.ShouldBe(template.Lines);
        template.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void ToRules_ShouldReturnRulesInSequenceOrder()
    {
        // Arrange
        var template = NewTemplate(
            new LedgerAllocationLine(BudgetA, 1, AllocationType.Fixed, Amount: 100),
            new LedgerAllocationLine(BudgetB, 0, AllocationType.Percent, Percent: 10));

        // Act
        var rules = template.ToRules();

        // Assert
        rules.ShouldBe([
            new AllocationRule(BudgetB, 0, AllocationType.Percent, 10),
            new AllocationRule(BudgetA, 1, AllocationType.Fixed, 100)]);
    }

    [Fact]
    public void MinimumAmount_WhenFixedStepFollowsPercentStep_ShouldBeTheSmallestAmountFundingIt()
    {
        // Arrange
        var template = NewTemplate(
            new LedgerAllocationLine(BudgetA, 0, AllocationType.Percent, Percent: 50),
            new LedgerAllocationLine(BudgetB, 1, AllocationType.Fixed, Amount: 100));

        // Act
        var minimum = template.MinimumAmount();

        // Assert
        minimum.ShouldBe(199.99m);
    }

    [Theory]
    [InlineData(AllocationType.Fixed, null, 10)]
    [InlineData(AllocationType.Percent, 10, null)]
    public void LineCreate_WhenValueOfItsTypeIsMissing_ShouldThrowArgumentNullException(AllocationType type, int? amount, int? percent)
    {
        // Arrange
        var line = new LedgerAllocationLine(BudgetA, 0, type, (decimal?)amount, (decimal?)percent);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => LedgerAllocationTemplateLine.Create(Guid.NewGuid(), line));
    }

    private static LedgerAllocationTemplate NewTemplate(params LedgerAllocationLine[] lines)
        => LedgerAllocationTemplate.Create(Guid.NewGuid(), "Template", "EUR", lines);
}
