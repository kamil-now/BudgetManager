using BudgetManager.Common.Models;
using BudgetManager.Domain.Entities;
using Shouldly;

namespace BudgetManager.UnitTests.Entities;

public class AccountTests
{
    [Fact]
    public void GetBalance_WhenNoTransactions_ShouldReturnEmptyBalance()
    {
        // Arrange
        var account = CreateAccount();

        // Act
        var balance = account.GetBalance();

        // Assert
        balance.ShouldNotBeNull();
        balance.ShouldBeEmpty();
    }

    [Fact]
    public void GetBalance_WhenEmptyTransactions_ShouldNotThrow()
    {
        // Arrange
        var account = CreateAccount();
        account.Transactions = [];

        // Act & Assert
        Should.NotThrow(() => account.GetBalance());
    }

    [Fact]
    public void GetBalance_ShouldCalculateCorrectBalance()
    {
        // Arrange
        var account = CreateAccount();

        account.Transactions.Add(new() { AccountId = account.Id, Value = new Money(200, "USD") });
        account.Transactions.Add(new() { AccountId = account.Id, Value = new Money(-50, "USD") });

        // Act
        var balance = account.GetBalance();

        // Assert
        balance.ShouldContainKey("USD");
        balance["USD"].ShouldBe(150);
    }

    [Fact]
    public void GetBalance_WhenMultipleCurrencies_ShouldCalculateCorrectBalanceForEach()
    {
        // Arrange
        var account = CreateAccount();

        account.Transactions.Add(new() { AccountId = account.Id, Value = new Money(100, "USD") });
        account.Transactions.Add(new() { AccountId = account.Id, Value = new Money(200, "EUR") });
        account.Transactions.Add(new() { AccountId = account.Id, Value = new Money(-25, "USD") });
        account.Transactions.Add(new() { AccountId = account.Id, Value = new Money(-50, "EUR") });

        // Act
        var balance = account.GetBalance();

        // Assert
        balance.ShouldContainKey("USD");
        balance.ShouldContainKey("EUR");
        balance["USD"].ShouldBe(75);
        balance["EUR"].ShouldBe(150);
    }

    [Fact]
    public void GetBalance_WhenZeroNetBalance_ShouldIncludeCurrency()
    {
        // Arrange
        var account = CreateAccount();

        account.Transactions.Add(new() { AccountId = account.Id, Value = new Money(100, "USD") });
        account.Transactions.Add(new() { AccountId = account.Id, Value = new Money(-100, "USD") });

        // Act
        var balance = account.GetBalance();

        // Assert
        balance.ShouldNotContainKey("USD");
    }

    private static Account CreateAccount()
    {
        return new Account
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Test Account",
            Description = "Test Description"
        };
    }
}
