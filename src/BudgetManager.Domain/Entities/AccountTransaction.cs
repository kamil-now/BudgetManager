using System.Linq.Expressions;
using BudgetManager.Domain.Models;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Domain.Entities;

public sealed class AccountTransaction : Entity, IAccessControlled<AccountTransaction>
{
    public required Guid AccountId { get; set; }
    public required Money Value { get; set; }
    public DateTimeOffset Date { get; set; }
    public string? Title { get; set; }
    public string? Comment { get; set; }
    public List<string>? Tags { get; set; }

    public AccountTransfer? InTransfer { get; set; }
    public AccountTransfer? OutTransfer { get; set; }
    public Account Account { get; set; } = null!;

    public static Expression<Func<AccountTransaction, Guid>> OwnerId => x => x.Account.Ledger.OwnerId;
}
