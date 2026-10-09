using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Models;
using Shouldly;

namespace BudgetManager.UnitTests.Domain;

public class BudgetTests
{
    [Fact]
    public void AddFund_ShouldAddFundToBudget()
    {
        // Arrange
        var budget = NewBudget();

        // Act
        budget.AddFund("Fund", "Description", []);

        // Assert
        var fund = budget.Funds.ShouldHaveSingleItem();
        fund.ShouldSatisfyAllConditions(
            x => x.BudgetId.ShouldBe(budget.Id),
            x => x.Name.ShouldBe("Fund"),
            x => x.Description.ShouldBe("Description"));
    }

    [Fact]
    public void AddFund_WhenAllocationsAreEmpty_ShouldNotAddAllocationTemplates()
    {
        // Arrange
        var budget = NewBudget();

        // Act
        budget.AddFund("Fund", null, []);

        // Assert
        budget.AllocationTemplates.ShouldBeEmpty();
    }

    [Fact]
    public void AddFund_WhenAllocationIsFixed_ShouldAddFixedLine()
    {
        // Arrange
        var budget = NewBudget();

        // Act
        budget.AddFund("Fund", null, [new("EUR", 3, AllocationType.Fixed, Amount: 100)]);

        // Assert
        var fund = budget.Funds.Single();
        var template = budget.AllocationTemplates.ShouldHaveSingleItem();
        template.ShouldSatisfyAllConditions(
            x => x.BudgetId.ShouldBe(budget.Id),
            x => x.Currency.ShouldBe("EUR"));
        template.Lines.ShouldHaveSingleItem().ShouldBeOfType<FixedAllocationTemplateLine>().ShouldSatisfyAllConditions(
            x => x.AllocationTemplateId.ShouldBe(template.Id),
            x => x.FundId.ShouldBe(fund.Id),
            x => x.Sequence.ShouldBe(3),
            x => x.Amount.ShouldBe(100));
    }

    [Fact]
    public void AddFund_WhenAllocationIsPercent_ShouldAddPercentLine()
    {
        // Arrange
        var budget = NewBudget();

        // Act
        budget.AddFund("Fund", null, [new("EUR", 3, AllocationType.Percent, Percent: 12.5m)]);

        // Assert
        var fund = budget.Funds.Single();
        var template = budget.AllocationTemplates.Single();
        template.Lines.ShouldHaveSingleItem().ShouldBeOfType<PercentAllocationTemplateLine>().ShouldSatisfyAllConditions(
            x => x.AllocationTemplateId.ShouldBe(template.Id),
            x => x.FundId.ShouldBe(fund.Id),
            x => x.Sequence.ShouldBe(3),
            x => x.Percent.ShouldBe(12.5m));
    }

    [Fact]
    public void AddFund_WhenTemplateForCurrencyExists_ShouldAddLineToIt()
    {
        // Arrange
        var budget = NewBudget();
        budget.AddFund("Fund A", null, [new("EUR", 0, AllocationType.Fixed, Amount: 100)]);

        // Act
        budget.AddFund("Fund B", null, [new("EUR", 1, AllocationType.Percent, Percent: 10)]);

        // Assert
        var template = budget.AllocationTemplates.ShouldHaveSingleItem();
        template.Lines.Select(x => x.FundId).ShouldBe(budget.Funds.Select(x => x.Id), ignoreOrder: true);
    }

    [Fact]
    public void AddFund_WhenAllocationsHaveDifferentCurrencies_ShouldAddTemplatePerCurrency()
    {
        // Arrange
        var budget = NewBudget();

        // Act
        budget.AddFund("Fund", null, [
            new("EUR", 0, AllocationType.Fixed, Amount: 100),
            new("USD", 0, AllocationType.Percent, Percent: 10)
        ]);

        // Assert
        budget.AllocationTemplates.Select(x => x.Currency).ShouldBe(["EUR", "USD"], ignoreOrder: true);
    }

    [Fact]
    public void AddFund_WhenAllocationTypeIsUnknown_ShouldThrowNotSupportedException()
    {
        // Arrange
        var budget = NewBudget();

        // Act & Assert
        Should.Throw<NotSupportedException>(() => budget.AddFund("Fund", null, [new FundAllocation("EUR", 0, (AllocationType)2)]));
    }

    private static Budget NewBudget() => new() { LedgerId = Guid.NewGuid(), Name = "Budget" };
}
