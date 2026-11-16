using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Models;
using Shouldly;

namespace BudgetManager.UnitTests.Domain;

public class AllocationTemplateTests
{
    private static readonly Guid FundA = Guid.NewGuid();
    private static readonly Guid FundB = Guid.NewGuid();

    [Fact]
    public void ReplaceLines_WhenLineIsUnchanged_ShouldStillReplaceIt()
    {
        // Arrange
        var template = NewTemplate(new AllocationLine(FundA, 0, AllocationType.Fixed, Amount: 100));
        var line = template.Lines.Single();

        // Act
        template.ReplaceLines([new AllocationLine(FundA, 0, AllocationType.Fixed, Amount: 100)]);

        // Assert
        template.Lines.ShouldHaveSingleItem().ShouldNotBeSameAs(line);
    }

    [Fact]
    public void ReplaceLines_WhenFundIsMissing_ShouldRemoveItsLine()
    {
        // Arrange
        var template = NewTemplate(
            new AllocationLine(FundA, 0, AllocationType.Fixed, Amount: 100),
            new AllocationLine(FundB, 1, AllocationType.Percent, Percent: 10));

        // Act
        template.ReplaceLines([new AllocationLine(FundB, 1, AllocationType.Percent, Percent: 10)]);

        // Assert
        template.Lines.ShouldHaveSingleItem().FundId.ShouldBe(FundB);
    }

    [Fact]
    public void ReplaceLines_ShouldAddALineForEachGivenLineAndReturnThem()
    {
        // Arrange
        var template = NewTemplate(new AllocationLine(FundA, 0, AllocationType.Fixed, Amount: 100));

        // Act
        var added = template.ReplaceLines([
            new AllocationLine(FundA, 0, AllocationType.Percent, Percent: 20),
            new AllocationLine(FundB, 1, AllocationType.Fixed, Amount: 10)]);

        // Assert
        added.ShouldBe(template.Lines, ignoreOrder: true);
        template.Lines.Select(x => x.ToRule()).ShouldBe([
            new AllocationRule(FundA, 0, AllocationType.Percent, 20),
            new AllocationRule(FundB, 1, AllocationType.Fixed, 10)], ignoreOrder: true);
        template.Lines.ShouldAllBe(x => x.AllocationTemplateId == template.Id);
    }

    [Fact]
    public void ReplaceLines_WhenFundIsRepeated_ShouldKeepALinePerRule()
    {
        // Arrange
        var template = NewTemplate(new AllocationLine(FundA, 0, AllocationType.Fixed, Amount: 100));

        // Act
        template.ReplaceLines([
            new AllocationLine(FundA, 0, AllocationType.Fixed, Amount: 50),
            new AllocationLine(FundA, 1, AllocationType.Percent, Percent: 10)]);

        // Assert
        template.ToRules().ShouldBe([
            new AllocationRule(FundA, 0, AllocationType.Fixed, 50),
            new AllocationRule(FundA, 1, AllocationType.Percent, 10)]);
    }

    [Fact]
    public void ReplaceLines_ShouldSetUpdatedAt()
    {
        // Arrange
        var template = NewTemplate(new AllocationLine(FundA, 0, AllocationType.Fixed, Amount: 100));

        // Act
        template.ReplaceLines([new AllocationLine(FundA, 0, AllocationType.Fixed, Amount: 200)]);

        // Assert
        template.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void ToRules_ShouldReturnRulesInSequenceOrder()
    {
        // Arrange
        var template = NewTemplate(
            new AllocationLine(FundA, 1, AllocationType.Fixed, Amount: 100),
            new AllocationLine(FundB, 0, AllocationType.Percent, Percent: 10));

        // Act
        var rules = template.ToRules();

        // Assert
        rules.ShouldBe([
            new AllocationRule(FundB, 0, AllocationType.Percent, 10),
            new AllocationRule(FundA, 1, AllocationType.Fixed, 100)]);
    }

    private static AllocationTemplate NewTemplate(params AllocationLine[] lines)
        => AllocationTemplate.Create(Guid.NewGuid(), "Template", "EUR", lines);

    [Fact]
    public void MinimumAmount_WhenFixedStepFollowsPercentStep_ShouldBeTheSmallestAmountFundingIt()
    {
        // Arrange
        var template = NewTemplate(
            new AllocationLine(FundA, 0, AllocationType.Percent, Percent: 33.33m),
            new AllocationLine(FundB, 1, AllocationType.Fixed, Amount: 100));

        // Act
        var minimum = template.MinimumAmount();

        // Assert
        minimum.ShouldBe(149.98m);
    }
}
