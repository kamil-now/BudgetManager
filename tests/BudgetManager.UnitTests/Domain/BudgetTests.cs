using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Models;
using Shouldly;

namespace BudgetManager.UnitTests.Domain;

public class BudgetTests
{
    [Fact]
    public void AddFund_ShouldAddFundToBudgetAndReturnIt()
    {
        // Arrange
        var budget = NewBudget();

        // Act
        var result = budget.AddFund("Fund");

        // Assert
        var fund = budget.Funds.ShouldHaveSingleItem();
        fund.ShouldBeSameAs(result);
        fund.ShouldSatisfyAllConditions(
            x => x.BudgetId.ShouldBe(budget.Id),
            x => x.Name.ShouldBe("Fund"));
    }

    [Fact]
    public void AddAllocationTemplate_ShouldAddNamedTemplateWithLines()
    {
        // Arrange
        var budget = NewBudget();
        var fund = budget.AddFund("Fund");

        // Act
        var result = budget.AddAllocationTemplate("Salary", "EUR", [
            new AllocationLine(fund.Id, 0, AllocationType.Fixed, Amount: 100),
            new AllocationLine(fund.Id, 1, AllocationType.Percent, Percent: 12.5m)]);

        // Assert
        var template = budget.AllocationTemplates.ShouldHaveSingleItem();
        template.ShouldBeSameAs(result);
        template.ShouldSatisfyAllConditions(
            x => x.BudgetId.ShouldBe(budget.Id),
            x => x.Name.ShouldBe("Salary"),
            x => x.Currency.ShouldBe("EUR"));
        template.ToRules().ShouldBe([
            new AllocationRule(fund.Id, 0, AllocationType.Fixed, 100),
            new AllocationRule(fund.Id, 1, AllocationType.Percent, 12.5m)]);
        template.Lines.ShouldAllBe(x => x.AllocationTemplateId == template.Id);
    }

    [Fact]
    public void AddAllocationTemplate_WhenCurrencyRepeats_ShouldAddAnotherTemplate()
    {
        // Arrange
        var budget = NewBudget();
        var fund = budget.AddFund("Fund");
        AllocationLine[] lines = [new(fund.Id, 0, AllocationType.Percent, Percent: 10)];

        // Act
        budget.AddAllocationTemplate("A", "EUR", lines);
        budget.AddAllocationTemplate("B", "EUR", lines);

        // Assert
        budget.AllocationTemplates.Select(x => x.Name).ShouldBe(["A", "B"]);
    }

    [Fact]
    public void AddAllocationTemplate_WhenAllocationTypeIsUnknown_ShouldThrowNotSupportedException()
    {
        // Arrange
        var budget = NewBudget();
        var fund = budget.AddFund("Fund");

        // Act & Assert
        Should.Throw<NotSupportedException>(() => budget.AddAllocationTemplate("A", "EUR", [new AllocationLine(fund.Id, 0, (AllocationType)2)]));
    }

    private static Budget NewBudget() => new() { LedgerId = Guid.NewGuid(), Name = "Budget" };
}
