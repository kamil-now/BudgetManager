using System.Linq.Expressions;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Interfaces;
using BudgetManager.Domain.Models;

namespace BudgetManager.Domain.Entities;

public sealed class AccountTransfer : Entity, IAccessControlled<AccountTransfer>
{
    public required Guid IncomeId { get; init; }
    public required Guid ExpenseId { get; init; }

    public AccountTransaction Income { get; private set; } = null!;
    public AccountTransaction Expense { get; private set; } = null!;

    public static Expression<Func<AccountTransfer, Guid>> OwnerId => x => x.Expense.Account.Ledger.OwnerId;

    public static AccountTransfer Transfer(
        Guid sourceAccountId,
        Guid targetAccountId,
        Money value,
        DateTimeOffset date,
        string? title,
        string? comment,
        IEnumerable<string>? tags)
    {
        EnsureValidTransfer(sourceAccountId, targetAccountId, value);
        return Create(sourceAccountId, -value, targetAccountId, value, date, title, comment, tags);
    }

    public static AccountTransfer Exchange(
        Guid sourceAccountId,
        Guid targetAccountId,
        Money sell,
        Money buy,
        DateTimeOffset date,
        string? title,
        string? comment,
        IEnumerable<string>? tags)
    {
        EnsureValidExchange(sell, buy);
        return Create(sourceAccountId, -sell, targetAccountId, buy, date, title, comment, tags);
    }

    public void UpdateTransfer(
        Guid sourceAccountId,
        Guid targetAccountId,
        Money value,
        DateTimeOffset date,
        string? title,
        string? comment,
        IEnumerable<string>? tags)
    {
        EnsureValidTransfer(sourceAccountId, targetAccountId, value);
        Set(sourceAccountId, -value, targetAccountId, value, date, title, comment, tags);
    }

    public void UpdateExchange(
        Guid sourceAccountId,
        Guid targetAccountId,
        Money sell,
        Money buy,
        DateTimeOffset date,
        string? title,
        string? comment,
        IEnumerable<string>? tags)
    {
        EnsureValidExchange(sell, buy);
        Set(sourceAccountId, -sell, targetAccountId, buy, date, title, comment, tags);
    }

    private static void EnsureValidTransfer(Guid sourceAccountId, Guid targetAccountId, Money value)
    {
        if (value.Amount <= 0)
        {
            throw new InvalidOperationException("Transfer value must be positive.");
        }
        if (sourceAccountId == targetAccountId)
        {
            throw new InvalidOperationException("Transfer target account must be different from the source account.");
        }
    }

    private static void EnsureValidExchange(Money sell, Money buy)
    {
        if (sell.Amount <= 0 || buy.Amount <= 0)
        {
            throw new InvalidOperationException("Exchange sell and buy values must be positive.");
        }
        if (sell.Currency == buy.Currency)
        {
            throw new InvalidOperationException("Exchange sell and buy values cannot have the same currency.");
        }
    }

    private void Set(
        Guid expenseAccountId,
        Money expenseValue,
        Guid incomeAccountId,
        Money incomeValue,
        DateTimeOffset date,
        string? title,
        string? comment,
        IEnumerable<string>? tags)
    {
        if (Expense is null || Income is null)
        {
            throw new InvalidOperationException($"Transactions of transfer {Id} are not loaded.");
        }

        Expense.Set(expenseAccountId, expenseValue, date, title, comment, tags);
        Income.Set(incomeAccountId, incomeValue, date, title, comment, tags);
    }

    private static AccountTransfer Create(
        Guid expenseAccountId,
        Money expenseValue,
        Guid incomeAccountId,
        Money incomeValue,
        DateTimeOffset date,
        string? title,
        string? comment,
        IEnumerable<string>? tags)
    {
        var expense = AccountTransaction.CreateTransfer(AccountTransactionType.TransferOut, expenseAccountId, expenseValue, date, title, comment, tags);
        var income = AccountTransaction.CreateTransfer(AccountTransactionType.TransferIn, incomeAccountId, incomeValue, date, title, comment, tags);

        return new AccountTransfer
        {
            ExpenseId = expense.Id,
            IncomeId = income.Id,
            Expense = expense,
            Income = income,
        };
    }
}
