using System.Linq.Expressions;
using BudgetManager.Domain.Interfaces;
using BudgetManager.Domain.Models;

namespace BudgetManager.Domain.Entities;

public class FundTransaction : Entity, IAccessControlled<FundTransaction>
{
    public required Guid FundId { get; set; }
    public required Money Value { get; set; }
    public DateTimeOffset Date { get; set; }
    public string? Title { get; set; }

    /// <summary>
    /// The income this allocation came from, or the expense that was paid from the fund (a negative value); the row is
    /// deleted with it. Exactly one of this and <see cref="AllocationId"/> is set.
    /// </summary>
    public Guid? AccountTransactionId { get; set; }

    /// <summary>
    /// The allocation operation this row belongs to; the row is deleted with it.
    /// </summary>
    public Guid? AllocationId { get; set; }

    public Fund Fund { get; set; } = null!;
    public AccountTransaction? AccountTransaction { get; set; }
    public Allocation? Allocation { get; set; }

    public static Expression<Func<FundTransaction, Guid>> OwnerId => x => x.Fund.Budget.Ledger.OwnerId;

    /// <summary>
    /// An allocation of <paramref name="source"/>, which takes its date and title and is deleted with it.
    /// </summary>
    public static FundTransaction Allocate(Guid fundId, Money value, AccountTransaction source) => new()
    {
        FundId = fundId,
        Value = value,
        Date = source.Date,
        Title = source.Title,
        AccountTransactionId = source.Id
    };

    /// <summary>
    /// A part of <paramref name="source"/>, which takes its date and is deleted with it.
    /// </summary>
    public static FundTransaction Allocate(Guid fundId, Money value, Allocation source) => new()
    {
        FundId = fundId,
        Value = value,
        Date = source.Date,
        AllocationId = source.Id
    };

    /// <summary>
    /// The payment of <paramref name="expense"/> from the fund: a negative row that is deleted with the expense.
    /// </summary>
    public static FundTransaction Pay(Guid fundId, AccountTransaction expense)
    {
        if (expense.Value.Amount >= 0)
        {
            throw new InvalidOperationException("Only an expense can be paid from a fund.");
        }

        return new FundTransaction
        {
            FundId = fundId,
            Value = expense.Value,
            Date = expense.Date,
            Title = expense.Title,
            AccountTransactionId = expense.Id
        };
    }
}
