using System.Linq.Expressions;
using BudgetManager.Domain.Interfaces;
using BudgetManager.Domain.Models;

namespace BudgetManager.Domain.Entities;

public sealed class Allocation : Entity, IAccessControlled<Allocation>
{
    public Guid LedgerId { get; private set; }
    public string Currency { get; private set; } = null!;
    public DateTimeOffset Date { get; private set; }

    /// <summary>
    /// The allocation made when the ledger was created; its date cannot be changed.
    /// </summary>
    public bool IsInitial { get; private set; }

    public Ledger Ledger { get; private set; } = null!;
    public List<FundTransaction> Transactions { get; private set; } = [];

    public decimal Total => Transactions.Sum(x => x.Value.Amount);

    public static Expression<Func<Allocation, Guid>> OwnerId => x => x.Ledger.OwnerId;

    private Allocation() { }

    public static Allocation Create(Guid ledgerId, string currency, DateTimeOffset date, IEnumerable<FundAmount> amounts)
        => New(ledgerId, currency, date, false, amounts);

    public static Allocation CreateInitial(Guid ledgerId, string currency, IEnumerable<FundAmount> amounts)
        => New(ledgerId, currency, default, true, amounts);

    public void Update(string currency, DateTimeOffset date)
    {
        if (IsInitial && date != Date)
        {
            throw new InvalidOperationException($"Initial allocation {Id} date cannot be changed.");
        }

        Currency = currency;
        Date = date;

        foreach (var transaction in Transactions)
        {
            transaction.Date = date;
        }
    }

    private static Allocation New(
        Guid ledgerId,
        string currency,
        DateTimeOffset date,
        bool isInitial,
        IEnumerable<FundAmount> amounts)
    {
        var allocation = new Allocation
        {
            LedgerId = ledgerId,
            Currency = currency,
            Date = date,
            IsInitial = isInitial
        };
        allocation.Transactions.AddRange(amounts.Select(x =>
            FundTransaction.Allocate(x.FundId, new Money(x.Amount, currency), allocation)));
        return allocation;
    }
}
