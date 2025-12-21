using BudgetManager.Common.Models;
using BudgetManager.Domain.Entities;
using Shouldly;

namespace BudgetManager.UnitTests.Entities;

public class FundTests
{
    [Fact]
    public void GetBalance_WhenNoTransactions_ShouldReturnEmptyBalance()
    {
        // Arrange
        var fund = CreateFund();

        // Act
        var balance = fund.GetBalance();

        // Assert
        balance.ShouldNotBeNull();
        balance.ShouldBeEmpty();
    }

    [Fact]
    public void GetBalance_WhenOnlyAllocations_ShouldReturnAllocationSum()
    {
        // Arrange
        var fund = CreateFund();
        fund.Allocations.Add(new Allocation { FundId = fund.Id, Amount = new Money(100, "USD") });
        fund.Allocations.Add(new Allocation { FundId = fund.Id, Amount = new Money(50, "USD") });

        // Act
        var balance = fund.GetBalance();

        // Assert
        balance.ShouldContainKey("USD");
        balance["USD"].ShouldBe(150);
    }

    [Fact]
    public void GetBalance_WhenOnlyDeallocations_ShouldReturnNegativeBalance()
    {
        // Arrange
        var fund = CreateFund();
        fund.Deallocations.Add(new Deallocation { FundId = fund.Id, Amount = new Money(100, "USD") });
        fund.Deallocations.Add(new Deallocation { FundId = fund.Id, Amount = new Money(50, "USD") });

        // Act
        var balance = fund.GetBalance();

        // Assert
        balance.ShouldContainKey("USD");
        balance["USD"].ShouldBe(-150);
    }

    [Fact]
    public void GetBalance_WhenIncomingReallocation_ShouldAddAmount()
    {
        // Arrange
        var fund = CreateFund();
        var sourceFund = CreateFund();

        fund.IncomingReallocations.Add(new Reallocation
        {
            FundId = sourceFund.Id,
            TargetFundId = fund.Id,
            Amount = new Money(100, "USD")
        });

        // Act
        var balance = fund.GetBalance();

        // Assert
        balance.ShouldContainKey("USD");
        balance["USD"].ShouldBe(100);
    }

    [Fact]
    public void GetBalance_WhenOutgoingReallocation_ShouldDeductAmount()
    {
        // Arrange
        var fund = CreateFund();
        var targetFund = CreateFund();

        fund.OutgoingReallocations.Add(new Reallocation
        {
            FundId = fund.Id,
            TargetFundId = targetFund.Id,
            Amount = new Money(100, "USD")
        });

        // Act
        var balance = fund.GetBalance();

        // Assert
        balance.ShouldContainKey("USD");
        balance["USD"].ShouldBe(-100);
    }

    [Fact]
    public void GetBalance_WhenMixedTransactions_ShouldCalculateCorrectBalance()
    {
        // Arrange
        var fund = CreateFund();
        var targetFund = CreateFund();

        fund.Allocations.Add(new Allocation { FundId = fund.Id, Amount = new Money(200, "USD") });
        fund.Deallocations.Add(new Deallocation { FundId = fund.Id, Amount = new Money(50, "USD") });
        fund.OutgoingReallocations.Add(new Reallocation
        {
            FundId = fund.Id,
            TargetFundId = targetFund.Id,
            Amount = new Money(30, "USD")
        });
        fund.IncomingReallocations.Add(new Reallocation
        {
            FundId = targetFund.Id,
            TargetFundId = fund.Id,
            Amount = new Money(20, "USD")
        });

        // Act
        var balance = fund.GetBalance();

        // Assert
        balance.ShouldContainKey("USD");
        balance["USD"].ShouldBe(140); // 200 - 50 - 30 + 20
    }

    [Fact]
    public void GetBalance_WhenMultipleCurrencies_ShouldCalculateCorrectBalanceForEach()
    {
        // Arrange
        var fund = CreateFund();

        fund.Allocations.Add(new Allocation { FundId = fund.Id, Amount = new Money(100, "USD") });
        fund.Allocations.Add(new Allocation { FundId = fund.Id, Amount = new Money(200, "EUR") });
        fund.Deallocations.Add(new Deallocation { FundId = fund.Id, Amount = new Money(25, "USD") });
        fund.Deallocations.Add(new Deallocation { FundId = fund.Id, Amount = new Money(50, "EUR") });

        // Act
        var balance = fund.GetBalance();

        // Assert
        balance.ShouldContainKey("USD");
        balance.ShouldContainKey("EUR");
        balance["USD"].ShouldBe(75);
        balance["EUR"].ShouldBe(150);
    }

    [Fact]
    public void GetBalance_WhenZeroNetBalance_ShouldNotIncludeCurrency()
    {
        // Arrange
        var fund = CreateFund();

        fund.Allocations.Add(new Allocation { FundId = fund.Id, Amount = new Money(100, "USD") });
        fund.Deallocations.Add(new Deallocation { FundId = fund.Id, Amount = new Money(100, "USD") });

        // Act
        var balance = fund.GetBalance();

        // Assert
        balance.ShouldNotContainKey("USD");
    }

    [Fact]
    public void GetBalance_WhenEmptyCollections_ShouldNotThrow()
    {
        // Arrange
        var fund = CreateFund();
        fund.Allocations = [];
        fund.Deallocations = [];
        fund.IncomingReallocations = [];

        // Act & Assert
        Should.NotThrow(() => fund.GetBalance());
    }

    private static Fund CreateFund()
    {
        return new Fund
        {
            Id = Guid.NewGuid(),
            BudgetId = Guid.NewGuid(),
            Name = "Test Fund",
            Description = "Test Description"
        };
    }
}
