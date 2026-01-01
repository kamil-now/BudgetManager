using System.Linq.Expressions;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;
using BudgetManager.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BudgetManager.Infrastructure.Persistence.Services;

public class BudgetManagerService(ApplicationDbContext dbContext) : IBudgetManagerService
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) => await dbContext.SaveChangesAsync(cancellationToken);

    public async Task<T> RunInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        if (dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            return await action();
        }
        var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await action();
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<LedgerTransactions> GetLedgerTransactionsAsync(Guid ledgerId, LedgerTransactionsFilters filters, CancellationToken cancellationToken)
    {
        var accounts = await dbContext.Accounts
             .AsNoTracking()
             .Where(x => (filters.AccountId != null && x.Id == filters.AccountId) || (filters.AccountId == null && x.LedgerId == ledgerId))
             .Select(x => new { x.Id, x.Name })
             .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        var budgets = await dbContext.Budgets
             .AsNoTracking()
             .Where(x => (filters.BudgetId != null && x.Id == filters.BudgetId) || (filters.BudgetId == null && x.LedgerId == ledgerId))
             .Select(x => new { x.Id, x.Name })
             .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        var accountTransactions = await dbContext.AccountTransactions
                .AsNoTracking()
                .Include(x => x.InTransfer)
                .Include(x => x.OutTransfer)
                .Where(x => accounts.Keys.Contains(x.AccountId))
                .Where(x => x.Date >= filters.From && x.Date <= filters.To)
                .ToArrayAsync(cancellationToken);

        var accountTransactionsIds = accountTransactions.Select(x => x.Id).ToArray();

        var accountTransfers = await dbContext.AccountTransfers
                .AsNoTracking()
                .Where(x => accountTransactionsIds.Contains(x.IncomeId))
                .ToArrayAsync(cancellationToken);

        var funds = await dbContext.Funds
             .AsNoTracking()
             .Where(x => (filters.FundId != null && x.Id == filters.FundId) || budgets.Keys.Contains(x.Id))
             .Select(x => new { x.Id, x.BudgetId, x.Name })
             .ToDictionaryAsync(x => x.Id, x => (x.BudgetId, x.Name), cancellationToken);

        var fundTransactions = await dbContext.FundTransactions
                .AsNoTracking()
                .Include(x => x.InTransfer)
                .Include(x => x.OutTransfer)
                .Include(x => x.Fund)
                .ThenInclude(x => x.Budget)
                .Where(x => funds.Keys.Contains(x.FundId))
                .Where(x => x.Date >= filters.From && x.Date <= filters.To)
                .ToArrayAsync(cancellationToken);

        var fundTransactionsIds = fundTransactions.Select(x => x.Id).ToArray();

        var fundTransfers = await dbContext.FundTransfers
                .AsNoTracking()
                .Where(x => fundTransactionsIds.Contains(x.AllocationId))
                .ToArrayAsync(cancellationToken);

        return new()
        {
            Accounts = accounts,
            AccountTransactions = accountTransactions,
            AccountTransfers = accountTransfers,

            Funds = funds,
            FundTransactions = fundTransactions,
            FundTransfers = fundTransfers,

            Budgets = budgets,
        };
    }

    public async Task<T> CreateAsync<T>(T entity, CancellationToken cancellationToken) where T : Entity
    {
        if (entity is null)
        {
            throw new ArgumentNullException(nameof(entity), "Entity cannot be null");
        }

        await dbContext.Set<T>().AddAsync(entity, cancellationToken);

        return entity;
    }

    public async Task<Ledger?> GetLedgerAsync(Expression<Func<Ledger, bool>> predicate, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return await dbContext.Ledgers
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Budgets)
            .ThenInclude(x => x.Funds)
            .Include(x => x.Accounts)
            .ThenInclude(x => x.Transactions)
            .SingleOrDefaultAsync(predicate, cancellationToken);
    }

    public async Task<T> GetAsync<T>(Guid id, CancellationToken cancellationToken) where T : Entity
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("ID cannot be empty", nameof(id));
        }

        return await dbContext.Set<T>().FindAsync([id], cancellationToken)
          ?? throw new KeyNotFoundException($"Entity of type '{typeof(T).Name}' with ID '{id}' not found.");
    }

    public async Task<IEnumerable<T>> GetAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken) where T : Entity
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return await dbContext.Set<T>()
            .Where(predicate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid?> GetOwnerIdAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : Entity, IAccessControlled
    {
        return await dbContext.Set<T>()
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => x.OwnerId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken) where T : Entity
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return await dbContext.Set<T>().AnyAsync(predicate, cancellationToken);
    }

    public async Task DeleteAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : Entity
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("ID cannot be empty", nameof(id));
        }

        var entity = await GetAsync<T>(id, cancellationToken);
        dbContext.Set<T>().Remove(entity);
    }
}
