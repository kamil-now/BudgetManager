using System.Linq.Expressions;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Interfaces;
using BudgetManager.Domain.Models;

namespace BudgetManager.Domain.Entities;

public sealed class AccountTransaction : Entity, IAccessControlled<AccountTransaction>
{
    public Guid AccountId { get; private set; }
    public Money Value { get; private set; } = null!;
    public DateTimeOffset Date { get; private set; }
    public string? Title { get; private set; }
    public AccountTransactionType Type { get; private set; }

    public AccountTransfer? InTransfer { get; private set; }
    public AccountTransfer? OutTransfer { get; private set; }
    public Account Account { get; private set; } = null!;

    public bool IsTransfer => Type is AccountTransactionType.TransferIn or AccountTransactionType.TransferOut;

    public static Expression<Func<AccountTransaction, Guid>> OwnerId => x => x.Account.Ledger.OwnerId;

    private AccountTransaction() { }

    public static AccountTransaction Create(Guid accountId, Money value, DateTimeOffset date, string? title)
        => New(AccountTransactionType.Regular, accountId, value, date, title);

    public static AccountTransaction CreateInitialBalance(Guid accountId, Money value)
        => New(AccountTransactionType.InitialBalance, accountId, value, default, Constants.InitialBalanceTransactionTitle);

    internal static AccountTransaction CreateTransfer(AccountTransactionType type, Guid accountId, Money value, DateTimeOffset date, string? title)
        => New(type, accountId, value, date, title);

    public void Update(Guid accountId, Money value, DateTimeOffset date, string? title)
    {
        if (IsTransfer)
        {
            throw new InvalidOperationException($"Transaction {Id} is part of a transfer and can only be changed through it.");
        }
        if (Type == AccountTransactionType.InitialBalance && accountId != AccountId)
        {
            throw new InvalidOperationException($"Initial balance {Id} cannot be moved to another account.");
        }
        if (Type == AccountTransactionType.InitialBalance && date != Date)
        {
            throw new InvalidOperationException($"Initial balance {Id} date cannot be changed.");
        }

        Set(accountId, value, date, title);
    }

    internal void Set(Guid accountId, Money value, DateTimeOffset date, string? title)
    {
        AccountId = accountId;
        Value = value;
        Date = date;
        Title = title;
    }

    private static AccountTransaction New(AccountTransactionType type, Guid accountId, Money value, DateTimeOffset date, string? title)
    {
        var transaction = new AccountTransaction { Type = type };
        transaction.Set(accountId, value, date, title);
        return transaction;
    }
}
