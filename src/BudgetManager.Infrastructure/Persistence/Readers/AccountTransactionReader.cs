using BudgetManager.Application.Interfaces;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BudgetManager.Infrastructure.Persistence.Readers;

public sealed class AccountTransactionReader(ApplicationDbContext dbContext) : IAccountTransactionReader
{
    public async Task<AccountTransactionDetails?> ReadAsync(Guid accountId, Guid id, CancellationToken cancellationToken = default)
        => await dbContext.AccountTransactions
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
                x.Comment,
                x.Tags))
            .SingleOrDefaultAsync(cancellationToken);
}
