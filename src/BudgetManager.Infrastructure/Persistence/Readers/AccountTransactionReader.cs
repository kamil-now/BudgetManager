using BudgetManager.Application.Interfaces;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BudgetManager.Infrastructure.Persistence.Readers;

public sealed class AccountTransactionReader(ApplicationDbContext dbContext) : IAccountTransactionReader
{
    public async Task<AccountTransactionDetails?> ReadAsync(Guid accountId, Guid id, CancellationToken cancellationToken = default)
    {
        var transaction = await dbContext.AccountTransactions
            .AsNoTracking()
            .Where(x => x.Id == id
                && x.AccountId == accountId
                && x.Type != AccountTransactionType.TransferIn
                && x.Type != AccountTransactionType.TransferOut)
            .Select(x => new AccountTransactionDetails(
                x.Id,
                x.AccountId,
                x.Account.Name,
                x.Value,
                x.Date,
                x.Title,
                Array.Empty<FundAmount>()))
            .SingleOrDefaultAsync(cancellationToken);

        if (transaction is null)
        {
            return null;
        }

        var linked = await dbContext.FundTransactions
            .AsNoTracking()
            .Where(x => x.AccountTransactionId == id)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new FundAmount(x.FundId, x.Value.Amount))
            .ToArrayAsync(cancellationToken);

        // positive rows are the allocations of an income, a negative one is the payment of an expense
        return transaction with
        {
            Allocations = linked.Where(x => x.Amount > 0).ToArray(),
            FundId = linked.FirstOrDefault(x => x.Amount < 0)?.FundId
        };
    }
}
