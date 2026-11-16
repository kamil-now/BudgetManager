using BudgetManager.Application.Interfaces;
using BudgetManager.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BudgetManager.Infrastructure.Persistence.Readers;

public sealed class AccountTransferReader(ApplicationDbContext dbContext) : IAccountTransferReader
{
    public async Task<AccountTransferDetails?> ReadAsync(Guid accountId, Guid id, CancellationToken cancellationToken = default)
        => await dbContext.AccountTransfers
            .AsNoTracking()
            .Where(x => x.Id == id
                && (x.Expense.AccountId == accountId || x.Income.AccountId == accountId))
            .Select(x => new AccountTransferDetails(
                x.Id,
                x.Expense.AccountId,
                x.Expense.Account.Name,
                x.Expense.Value,
                x.Income.AccountId,
                x.Income.Account.Name,
                x.Income.Value,
                x.Income.Date,
                x.Income.Title))
            .SingleOrDefaultAsync(cancellationToken);
}
