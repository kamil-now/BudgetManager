using BudgetManager.Application.Interfaces;
using BudgetManager.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BudgetManager.Infrastructure.Persistence.Readers;

public sealed class LedgerTransactionsReader(ApplicationDbContext dbContext) : ILedgerTransactionsReader
{
    public async Task<LedgerTransactions> ReadAsync(Guid ledgerId, LedgerTransactionsFilters filters, CancellationToken cancellationToken = default)
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
}
