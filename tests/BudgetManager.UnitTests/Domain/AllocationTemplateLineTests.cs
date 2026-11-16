using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using Shouldly;

namespace BudgetManager.UnitTests.Domain;

public class AllocationTemplateLineTests
{
    private static readonly Guid TemplateId = Guid.NewGuid();
    private static readonly Guid FundId = Guid.NewGuid();

    [Fact]
    public void Create_WhenTypeIsFixed_ShouldCreateFixedLine()
    {
        // Act
        var line = AllocationTemplateLine.Create(TemplateId, FundId, 2, AllocationType.Fixed, 150, null);

        // Assert
        line.ShouldBeOfType<FixedAllocationTemplateLine>().ShouldSatisfyAllConditions(
            x => x.AllocationTemplateId.ShouldBe(TemplateId),
            x => x.FundId.ShouldBe(FundId),
            x => x.Sequence.ShouldBe(2),
            x => x.Amount.ShouldBe(150));
    }

    [Fact]
    public void Create_WhenTypeIsPercent_ShouldCreatePercentLine()
    {
        // Act
        var line = AllocationTemplateLine.Create(TemplateId, FundId, 3, AllocationType.Percent, null, 12.5m);

        // Assert
        line.ShouldBeOfType<PercentAllocationTemplateLine>().ShouldSatisfyAllConditions(
            x => x.AllocationTemplateId.ShouldBe(TemplateId),
            x => x.FundId.ShouldBe(FundId),
            x => x.Sequence.ShouldBe(3),
            x => x.Percent.ShouldBe(12.5m));
    }

    [Fact]
    public void Create_WhenFixedLineHasNoAmount_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => AllocationTemplateLine.Create(TemplateId, FundId, 0, AllocationType.Fixed, null, 10));
    }

    [Fact]
    public void Create_WhenPercentLineHasNoPercent_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => AllocationTemplateLine.Create(TemplateId, FundId, 0, AllocationType.Percent, 10, null));
    }

    [Fact]
    public void Create_WhenTypeIsUnknown_ShouldThrowNotSupportedException()
    {
        // Act & Assert
        Should.Throw<NotSupportedException>(() => AllocationTemplateLine.Create(TemplateId, FundId, 0, (AllocationType)2, 10, 10));
    }
}
