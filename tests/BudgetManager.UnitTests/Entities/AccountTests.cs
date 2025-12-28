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
    public void GetBalance_WhenOnlyIncomes_ShouldReturnIncomeSum()
    {
        // Arrange
        var account = CreateAccount();
        account.Incomes.Add(new Income { AccountId = account.Id, Amount = new Money(100, "USD") });
        account.Incomes.Add(new Income { AccountId = account.Id, Amount = new Money(50, "USD") });

        // Act
        var balance = account.GetBalance();

        // Assert
        balance.ShouldContainKey("USD");
        balance["USD"].ShouldBe(150);
    }

    [Fact]
    public void GetBalance_WhenOnlyExpenses_ShouldReturnNegativeBalance()
    {
        // Arrange
        var account = CreateAccount();
        account.Expenses.Add(new Expense { AccountId = account.Id, Amount = new Money(100, "USD") });
        account.Expenses.Add(new Expense { AccountId = account.Id, Amount = new Money(50, "USD") });

        // Act
        var balance = account.GetBalance();

        // Assert
        balance.ShouldContainKey("USD");
        balance["USD"].ShouldBe(-150);
    }

    [Fact]
    public void GetBalance_WhenIncomingTransfer_ShouldAddAmount()
    {
        // Arrange
        var account = CreateAccount();
        var sourceAccount = CreateAccount();

        account.IncomingTransfers.Add(new Transfer
        {
            AccountId = sourceAccount.Id,
            TargetAccountId = account.Id,
            Amount = new Money(100, "USD")
        });

        // Act
        var balance = account.GetBalance();

        // Assert
        balance.ShouldContainKey("USD");
        balance["USD"].ShouldBe(100);
    }

    [Fact]
    public void GetBalance_WhenOutgoingTransfer_ShouldDeductAmount()
    {
        // Arrange
        var account = CreateAccount();
        var targetAccount = CreateAccount();

        account.OutgoingTransfers.Add(new Transfer
        {
            AccountId = account.Id,
            TargetAccountId = targetAccount.Id,
            Amount = new Money(100, "USD")
        });

        // Act
        var balance = account.GetBalance();

        // Assert
        balance.ShouldContainKey("USD");
        balance["USD"].ShouldBe(-100);
    }

    [Fact]
    public void GetBalance_WhenMixedTransactions_ShouldCalculateCorrectBalance()
    {
        // Arrange
        var account = CreateAccount();
        var targetAccount = CreateAccount();

        account.Incomes.Add(new Income { AccountId = account.Id, Amount = new Money(200, "USD") });
        account.Expenses.Add(new Expense { AccountId = account.Id, Amount = new Money(50, "USD") });
        account.OutgoingTransfers.Add(new Transfer
        {
            AccountId = account.Id,
            TargetAccountId = targetAccount.Id,
            Amount = new Money(30, "USD")
        });
        account.IncomingTransfers.Add(new Transfer
        {
            AccountId = targetAccount.Id,
            TargetAccountId = account.Id,
            Amount = new Money(20, "USD")
        });

        // Act
        var balance = account.GetBalance();

        // Assert
        balance.ShouldContainKey("USD");
        balance["USD"].ShouldBe(140); // 200 - 50 - 30 + 20
    }

    [Fact]
    public void GetBalance_WhenMultipleCurrencies_ShouldCalculateCorrectBalanceForEach()
    {
        // Arrange
        var account = CreateAccount();

        account.Incomes.Add(new Income { AccountId = account.Id, Amount = new Money(100, "USD") });
        account.Incomes.Add(new Income { AccountId = account.Id, Amount = new Money(200, "EUR") });
        account.Expenses.Add(new Expense { AccountId = account.Id, Amount = new Money(25, "USD") });
        account.Expenses.Add(new Expense { AccountId = account.Id, Amount = new Money(50, "EUR") });

        // Act
        var balance = account.GetBalance();

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
        var account = CreateAccount();

        account.Incomes.Add(new Income { AccountId = account.Id, Amount = new Money(100, "USD") });
        account.Expenses.Add(new Expense { AccountId = account.Id, Amount = new Money(100, "USD") });

        // Act
        var balance = account.GetBalance();

        // Assert
        balance.ShouldNotContainKey("USD");
    }

    [Fact]
    public void GetBalance_WhenEmptyCollections_ShouldNotThrow()
    {
        // Arrange
        var account = CreateAccount();
        account.Incomes = [];
        account.Expenses = [];
        account.IncomingTransfers = [];

        // Act & Assert
        Should.NotThrow(() => account.GetBalance());
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
