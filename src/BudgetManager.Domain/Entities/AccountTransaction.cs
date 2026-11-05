using System.Linq.Expressions;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Models;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Domain.Entities;

public sealed class AccountTransaction : Entity, IAccessControlled<AccountTransaction>
{
    public Guid AccountId { get; private set; }
    public Money Value { get; private set; } = null!;
    public DateTimeOffset Date { get; private set; }
    public string? Title { get; private set; }
    public string? Comment { get; private set; }
    public List<string>? Tags { get; private set; }
    public AccountTransactionType Type { get; private set; }

    public AccountTransfer? InTransfer { get; private set; }
    public AccountTransfer? OutTransfer { get; private set; }
    public Account Account { get; private set; } = null!;

    public bool IsTransfer => Type is AccountTransactionType.TransferIn or AccountTransactionType.TransferOut;

    public static Expression<Func<AccountTransaction, Guid>> OwnerId => x => x.Account.Ledger.OwnerId;

    private AccountTransaction() { }

    public static AccountTransaction Create(Guid accountId, Money value, DateTimeOffset date, string? title, string? comment = null, IEnumerable<string>? tags = null)
        => New(AccountTransactionType.Regular, accountId, value, date, title, comment, tags);

    public static AccountTransaction CreateInitialBalance(Guid accountId, Money value)
        => New(AccountTransactionType.InitialBalance, accountId, value, default, Constants.InitialBalanceTransactionTitle, null, null);

    internal static AccountTransaction CreateTransfer(AccountTransactionType type, Guid accountId, Money value, DateTimeOffset date, string? title, string? comment, IEnumerable<string>? tags)
        => New(type, accountId, value, date, title, comment, tags);

    public void Update(Guid accountId, Money value, DateTimeOffset date, string? title, string? comment, IEnumerable<string>? tags)
    {
        if (IsTransfer)
        {
            throw new InvalidOperationException($"Transaction {Id} is part of a transfer and can only be changed through it.");
        }
        if (Type == AccountTransactionType.InitialBalance && accountId != AccountId)
        {
            throw new InvalidOperationException($"Initial balance {Id} cannot be moved to another account.");
        }

        Set(accountId, value, date, title, comment, tags);
    }

    internal void Set(Guid accountId, Money value, DateTimeOffset date, string? title, string? comment, IEnumerable<string>? tags)
    {
        AccountId = accountId;
        Value = value;
        Date = date;
        Title = title;
        Comment = comment;
        Tags = tags?.ToList();
    }

    private static AccountTransaction New(AccountTransactionType type, Guid accountId, Money value, DateTimeOffset date, string? title, string? comment, IEnumerable<string>? tags)
    {
        var transaction = new AccountTransaction { Type = type };
        transaction.Set(accountId, value, date, title, comment, tags);
        return transaction;
    }
}
