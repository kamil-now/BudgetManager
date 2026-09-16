using System.Linq.Expressions;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;
using BudgetManager.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BudgetManager.Infrastructure.Persistence.Services;

public class BudgetManagerService(ApplicationDbContext dbContext) : IBudgetManagerService
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConflictException(ConcurrencyMessage(ex));
        }
    }

    private static string ConcurrencyMessage(DbUpdateConcurrencyException ex)
    {
        var entry = ex.Entries.FirstOrDefault();

        if (entry?.Entity is not Entity entity)
            return "The record was modified by another request.";

        return $"{entry.Entity.GetType().Name} {entity.Id} was modified by another request.";
    }

    public async Task<T> RunInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

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
             .Where(x => x.LedgerId == ledgerId && (filters.AccountId == null || x.Id == filters.AccountId))
             .Select(x => new { x.Id, x.Name })
             .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        var budgets = await dbContext.Budgets
             .AsNoTracking()
             .Where(x => x.LedgerId == ledgerId && (filters.BudgetId == null || x.Id == filters.BudgetId))
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
                .Where(x => accountTransactionsIds.Contains(x.IncomeId) || accountTransactionsIds.Contains(x.ExpenseId))
                .ToArrayAsync(cancellationToken);

        var missingAccountTransactionsIds = accountTransfers
                .SelectMany(x => new[] { x.IncomeId, x.ExpenseId })
                .Except(accountTransactionsIds)
                .ToArray();

        if (missingAccountTransactionsIds.Length > 0)
        {
            accountTransactions = [.. accountTransactions, .. await dbContext.AccountTransactions
                .AsNoTracking()
                .Include(x => x.InTransfer)
                .Include(x => x.OutTransfer)
                .Where(x => missingAccountTransactionsIds.Contains(x.Id))
                .ToArrayAsync(cancellationToken)];
        }

        var missingAccountIds = accountTransactions
                .Select(x => x.AccountId)
                .Where(x => !accounts.ContainsKey(x))
                .Distinct()
                .ToArray();

        if (missingAccountIds.Length > 0)
        {
            foreach (var account in await dbContext.Accounts
                .AsNoTracking()
                .Where(x => missingAccountIds.Contains(x.Id))
                .Select(x => new { x.Id, x.Name })
                .ToArrayAsync(cancellationToken))
                accounts[account.Id] = account.Name;
        }

        var funds = await dbContext.Funds
             .AsNoTracking()
             .Where(x => (filters.FundId != null && x.Id == filters.FundId && x.Budget.LedgerId == ledgerId) || budgets.Keys.Contains(x.BudgetId))
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
                .Where(x => fundTransactionsIds.Contains(x.AllocationId) || fundTransactionsIds.Contains(x.DeallocationId))
                .ToArrayAsync(cancellationToken);

        var missingFundTransactionsIds = fundTransfers
                .SelectMany(x => new[] { x.AllocationId, x.DeallocationId })
                .Except(fundTransactionsIds)
                .ToArray();

        if (missingFundTransactionsIds.Length > 0)
        {
            fundTransactions = [.. fundTransactions, .. await dbContext.FundTransactions
                .AsNoTracking()
                .Include(x => x.InTransfer)
                .Include(x => x.OutTransfer)
                .Include(x => x.Fund)
                .ThenInclude(x => x.Budget)
                .Where(x => missingFundTransactionsIds.Contains(x.Id))
                .ToArrayAsync(cancellationToken)];
        }

        var missingFundIds = fundTransactions
                .Select(x => x.FundId)
                .Where(x => !funds.ContainsKey(x))
                .Distinct()
                .ToArray();

        if (missingFundIds.Length > 0)
        {
            foreach (var fund in await dbContext.Funds
                .AsNoTracking()
                .Where(x => missingFundIds.Contains(x.Id))
                .Select(x => new { x.Id, x.BudgetId, x.Name })
                .ToArrayAsync(cancellationToken))
                funds[fund.Id] = (fund.BudgetId, fund.Name);
        }

        var missingBudgetIds = funds.Values
                .Select(x => x.BudgetId)
                .Where(x => !budgets.ContainsKey(x))
                .Distinct()
                .ToArray();

        if (missingBudgetIds.Length > 0)
        {
            foreach (var budget in await dbContext.Budgets
                .AsNoTracking()
                .Where(x => missingBudgetIds.Contains(x.Id))
                .Select(x => new { x.Id, x.Name })
                .ToArrayAsync(cancellationToken))
                budgets[budget.Id] = budget.Name;
        }

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

    public async Task<IEnumerable<AccountTransaction>> GetLedgerIncomesExpensesAsync(Guid ledgerId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken)
    {
        var accounts = await dbContext.Accounts
             .AsNoTracking()
             .Where(x => x.LedgerId == ledgerId)
             .Select(x => new { x.Id, x.Name })
             .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        var accountTransactions = await dbContext.AccountTransactions
                .AsNoTracking()
                .Include(x => x.InTransfer)
                .Include(x => x.OutTransfer)
                .Where(x => x.InTransfer == null && x.OutTransfer == null && accounts.Keys.Contains(x.AccountId))
                .Where(x => (from == null || x.Date >= from) && (to == null || x.Date <= to))
                .ToArrayAsync(cancellationToken);

        return accountTransactions;
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
            .Include(x => x.Accounts.OrderBy(a => a.Name))
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

    public async Task<Guid?> GetOwnerIdAsync<T>(Guid id, Expression<Func<T, Guid>> ownerSelector, CancellationToken cancellationToken = default) where T : Entity
    {
        var ownerIds = await dbContext.Set<T>()
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(ownerSelector)
            .ToArrayAsync(cancellationToken);

        return ownerIds.Cast<Guid?>().SingleOrDefault();
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
