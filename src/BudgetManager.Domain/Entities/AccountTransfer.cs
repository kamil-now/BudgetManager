using BudgetManager.Domain.Models;

namespace BudgetManager.Domain.Entities;

public sealed class AccountTransfer : Entity
{
    public required Guid IncomeId { get; set; }
    public required Guid ExpenseId { get; set; }

    public AccountTransaction Income { get; set; } = null!;
    public AccountTransaction Expense { get; set; } = null!;

    public static AccountTransfer Transfer(
        Guid sourceAccountId,
        Guid targetAccountId,
        Money value,
        DateTimeOffset date,
        string? title,
        string? comment)
        => Create(sourceAccountId, -value, targetAccountId, value, date, title, comment);

    public static AccountTransfer Exchange(
        Guid sourceAccountId,
        Guid targetAccountId,
        Money sell,
        Money buy,
        DateTimeOffset date,
        string? title,
        string? comment)
        => Create(sourceAccountId, sell, targetAccountId, buy, date, title, comment);

    private static AccountTransfer Create(
        Guid expenseAccountId,
        Money expenseValue,
        Guid incomeAccountId,
        Money incomeValue,
        DateTimeOffset date,
        string? title,
        string? comment)
    {
        var expense = new AccountTransaction
        {
            AccountId = expenseAccountId,
            Value = expenseValue,
            Date = date,
            Title = title,
            Comment = comment,
        };

        var income = new AccountTransaction
        {
            AccountId = incomeAccountId,
            Value = incomeValue,
            Date = date,
            Title = title,
            Comment = comment,
        };

        return new AccountTransfer
        {
            ExpenseId = expense.Id,
            IncomeId = income.Id,
            Expense = expense,
            Income = income,
        };
    }
}
