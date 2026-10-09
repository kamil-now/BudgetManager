using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit.Abstractions;

namespace BudgetManager.IntegrationTests.Persistence;

public class BudgetPersistenceTests(ITestOutputHelper testOutputHelper, PersistenceFixture fixture) : BaseTest(testOutputHelper, fixture)
{
    [Fact]
    public async Task Delete_WhenBudgetHasFunds_DeletesFunds()
    {
        // Arrange
        var (user, ledger, budget) = NewBudget();
        var fund = new Fund
        {
            BudgetId = budget.Id,
            Name = $"Fund {Guid.NewGuid()}"
        };
        var dbContext = GetContext();
        dbContext.Users.Add(user);
        dbContext.Ledgers.Add(ledger);
        dbContext.Budgets.Add(budget);
        dbContext.Funds.Add(fund);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        // Act
        var other = GetContext();
        other.Budgets.Remove(await other.Budgets.SingleAsync(x => x.Id == budget.Id));
        await other.SaveChangesAsync(CancellationToken.None);

        // Assert
        var result = GetContext();
        (await result.Funds.AnyAsync(x => x.Id == fund.Id)).ShouldBeFalse();
    }

    [Fact]
    public async Task Delete_WhenBudgetHasAllocationTemplates_DeletesTemplatesAndLines()
    {
        // Arrange
        var (user, ledger, budget) = NewBudget();
        var (fund, template, line) = NewAllocation(budget);
        var dbContext = GetContext();
        dbContext.Users.Add(user);
        dbContext.Ledgers.Add(ledger);
        dbContext.Budgets.Add(budget);
        dbContext.Funds.Add(fund);
        dbContext.AllocationTemplates.Add(template);
        dbContext.AllocationTemplateLines.Add(line);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        // Act
        var other = GetContext();
        other.Budgets.Remove(await other.Budgets.SingleAsync(x => x.Id == budget.Id));
        await other.SaveChangesAsync(CancellationToken.None);

        // Assert
        var result = GetContext();
        (await result.AllocationTemplates.AnyAsync(x => x.Id == template.Id)).ShouldBeFalse();
        (await result.AllocationTemplateLines.AnyAsync(x => x.Id == line.Id)).ShouldBeFalse();
    }

    [Fact]
    public async Task Delete_WhenFundHasAllocationTemplateLines_DeletesLines()
    {
        // Arrange
        var (user, ledger, budget) = NewBudget();
        var (fund, template, line) = NewAllocation(budget);
        var dbContext = GetContext();
        dbContext.Users.Add(user);
        dbContext.Ledgers.Add(ledger);
        dbContext.Budgets.Add(budget);
        dbContext.Funds.Add(fund);
        dbContext.AllocationTemplates.Add(template);
        dbContext.AllocationTemplateLines.Add(line);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        // Act
        var other = GetContext();
        other.Funds.Remove(await other.Funds.SingleAsync(x => x.Id == fund.Id));
        await other.SaveChangesAsync(CancellationToken.None);

        // Assert
        var result = GetContext();
        (await result.AllocationTemplateLines.AnyAsync(x => x.Id == line.Id)).ShouldBeFalse();
    }

    [Fact]
    public async Task Add_SavesAllocationTemplateLinesByType()
    {
        // Arrange
        var (user, ledger, budget) = NewBudget();
        var fixedFund = new Fund { BudgetId = budget.Id, Name = $"Fund {Guid.NewGuid()}" };
        var percentFund = new Fund { BudgetId = budget.Id, Name = $"Fund {Guid.NewGuid()}" };
        var template = new AllocationTemplate { BudgetId = budget.Id, Currency = "EUR" };
        var fixedLine = new FixedAllocationTemplateLine { AllocationTemplateId = template.Id, FundId = fixedFund.Id, Sequence = 0, Amount = 123.45m };
        var percentLine = new PercentAllocationTemplateLine { AllocationTemplateId = template.Id, FundId = percentFund.Id, Sequence = 1, Percent = 12.34m };
        var dbContext = GetContext();
        dbContext.Users.Add(user);
        dbContext.Ledgers.Add(ledger);
        dbContext.Budgets.Add(budget);
        dbContext.Funds.AddRange(fixedFund, percentFund);
        dbContext.AllocationTemplates.Add(template);

        // Act
        dbContext.AllocationTemplateLines.AddRange(fixedLine, percentLine);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        // Assert
        var result = GetContext();
        (await result.AllocationTemplateLines.SingleAsync(x => x.Id == fixedLine.Id))
            .ShouldBeOfType<FixedAllocationTemplateLine>().Amount.ShouldBe(123.45m);
        (await result.AllocationTemplateLines.SingleAsync(x => x.Id == percentLine.Id))
            .ShouldBeOfType<PercentAllocationTemplateLine>().Percent.ShouldBe(12.34m);
    }

    [Fact]
    public async Task Add_WhenBudgetAlreadyHasAllocationTemplateInCurrency_ThrowsException()
    {
        // Arrange
        var (user, ledger, budget) = NewBudget();
        var dbContext = GetContext();
        dbContext.Users.Add(user);
        dbContext.Ledgers.Add(ledger);
        dbContext.Budgets.Add(budget);
        dbContext.AllocationTemplates.Add(new AllocationTemplate { BudgetId = budget.Id, Currency = "EUR" });
        await dbContext.SaveChangesAsync(CancellationToken.None);

        // Act & Assert
        var other = GetContext();
        other.AllocationTemplates.Add(new AllocationTemplate { BudgetId = budget.Id, Currency = "EUR" });
        await Should.ThrowAsync<DbUpdateException>(() => other.SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Add_WhenFundAlreadyHasLineInAllocationTemplate_ThrowsException()
    {
        // Arrange
        var (user, ledger, budget) = NewBudget();
        var (fund, template, line) = NewAllocation(budget);
        var dbContext = GetContext();
        dbContext.Users.Add(user);
        dbContext.Ledgers.Add(ledger);
        dbContext.Budgets.Add(budget);
        dbContext.Funds.Add(fund);
        dbContext.AllocationTemplates.Add(template);
        dbContext.AllocationTemplateLines.Add(line);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        // Act & Assert
        var other = GetContext();
        other.AllocationTemplateLines.Add(new PercentAllocationTemplateLine { AllocationTemplateId = template.Id, FundId = fund.Id, Sequence = 1, Percent = 10 });
        await Should.ThrowAsync<DbUpdateException>(() => other.SaveChangesAsync(CancellationToken.None));
    }

    private static (User User, Ledger Ledger, Budget Budget) NewBudget()
    {
        var user = new User
        {
            Name = "Test User",
            Email = $"test@email{Guid.NewGuid()}",
            HashedPassword = "Test Hashed Password"
        };
        var ledger = new Ledger { OwnerId = user.Id, Name = $"Ledger {Guid.NewGuid()}" };
        var budget = new Budget { LedgerId = ledger.Id, Name = $"Budget {Guid.NewGuid()}" };
        return (user, ledger, budget);
    }

    private static (Fund Fund, AllocationTemplate Template, AllocationTemplateLine Line) NewAllocation(Budget budget)
    {
        var fund = new Fund { BudgetId = budget.Id, Name = $"Fund {Guid.NewGuid()}" };
        var template = new AllocationTemplate { BudgetId = budget.Id, Currency = "EUR" };
        var line = new FixedAllocationTemplateLine { AllocationTemplateId = template.Id, FundId = fund.Id, Sequence = 0, Amount = 100 };
        return (fund, template, line);
    }
}
