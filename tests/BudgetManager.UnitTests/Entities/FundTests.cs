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
    public void GetBalance_WhenEmptyTransactions_ShouldNotThrow()
    {
        // Arrange
        var fund = CreateFund();
        fund.Transactions = [];

        // Act & Assert
        Should.NotThrow(() => fund.GetBalance());
    }

    [Fact]
    public void GetBalance_WhenMixedTransactions_ShouldCalculateCorrectBalance()
    {
        // Arrange
        var fund = CreateFund();

        fund.Transactions.Add(new() { FundId = fund.Id, Value = new Money(200, "USD") });
        fund.Transactions.Add(new() { FundId = fund.Id, Value = new Money(-50, "USD") });

        // Act
        var balance = fund.GetBalance();

        // Assert
        balance.ShouldContainKey("USD");
        balance["USD"].ShouldBe(150);
    }

    [Fact]
    public void GetBalance_WhenMultipleCurrencies_ShouldCalculateCorrectBalanceForEach()
    {
        // Arrange
        var fund = CreateFund();

        fund.Transactions.Add(new() { FundId = fund.Id, Value = new Money(100, "USD") });
        fund.Transactions.Add(new() { FundId = fund.Id, Value = new Money(200, "EUR") });
        fund.Transactions.Add(new() { FundId = fund.Id, Value = new Money(-25, "USD") });
        fund.Transactions.Add(new() { FundId = fund.Id, Value = new Money(-50, "EUR") });

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

        fund.Transactions.Add(new() { FundId = fund.Id, Value = new Money(100, "USD") });
        fund.Transactions.Add(new() { FundId = fund.Id, Value = new Money(-100, "USD") });

        // Act
        var balance = fund.GetBalance();

        // Assert
        balance.ShouldNotContainKey("USD");
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
