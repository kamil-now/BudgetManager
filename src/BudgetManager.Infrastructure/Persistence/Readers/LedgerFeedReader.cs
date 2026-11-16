using BudgetManager.Application.Interfaces;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BudgetManager.Infrastructure.Persistence.Readers;

/// <summary>
/// Reads the feed in SQL: the incomes and expenses, the transfers and exchanges, and the allocations
/// are projected to one flat row each and joined with UNION ALL, then filtered, ordered and paged by the database.
/// The union is raw SQL because its branches fill different columns and leave the rest as typed NULLs, which EF's set
/// operations do not translate reliably; the filters, the ordering and the paging are composed on top of it with LINQ.
/// </summary>
public sealed class LedgerFeedReader(ApplicationDbContext dbContext) : ILedgerFeedReader
{
    public async Task<LedgerFeed> ReadAsync(Guid ledgerId, LedgerFeedFilters filters, CancellationToken cancellationToken = default)
    {
        var rows = Filter(Rows(ledgerId), filters);

        var total = await rows.CountAsync(cancellationToken);

        var page = await rows
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip(filters.Offset)
            .Take(filters.Limit)
            .ToArrayAsync(cancellationToken);

        var ownerIds = page
            .Where(x => x.Type is (int)FeedItemType.Income or (int)FeedItemType.Allocation)
            .Select(x => (Guid?)x.Id)
            .ToArray();

        // the page's incomes and allocations, with all of their lines whatever the fund scope; the payment of an expense
        // is a negative row and is not a line
        var linesByOwner = (await dbContext.FundTransactions
                .AsNoTracking()
                .Where(x => x.Value.Amount > 0 && (ownerIds.Contains(x.AccountTransactionId) || ownerIds.Contains(x.AllocationId)))
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .Select(x => new
                {
                    OwnerId = x.AccountTransactionId ?? x.AllocationId!.Value,
                    Allocation = new LedgerFeed.Allocation(x.Id, x.FundId, x.Fund.Name, x.Fund.BudgetId, x.Fund.Budget.Name, x.Value)
                })
                .ToArrayAsync(cancellationToken))
            .ToLookup(x => x.OwnerId, x => x.Allocation);

        return new LedgerFeed([.. page.Select(x => ToItem(x, [.. linesByOwner[x.Id]]))], total);
    }

    private static LedgerFeed.Item ToItem(FeedRow row, IReadOnlyList<LedgerFeed.Allocation> allocations) => new(
        row.Id,
        (FeedItemType)row.Type,
        row.Date,
        row.Title,
        row.AccountId,
        row.AccountName,
        row.TargetAccountId,
        row.TargetAccountName,
        row.BudgetId,
        row.BudgetName,
        row.FundId,
        row.FundName,
        new Money(row.Amount, row.Currency),
        row.ReceivedAmount is { } received && row.ReceivedCurrency is { } currency ? new Money(received, currency) : null,
        row.IsInitialBalance,
        allocations);

    private IQueryable<FeedRow> Filter(IQueryable<FeedRow> rows, LedgerFeedFilters filters)
    {
        if (filters.From is { } from)
        {
            rows = rows.Where(x => x.Date >= from);
        }
        if (filters.To is { } to)
        {
            rows = rows.Where(x => x.Date <= to);
        }
        if (filters.Types is { Length: > 0 } types)
        {
            var codes = types.Select(x => (int)x).Distinct().ToArray();
            rows = rows.Where(x => codes.Contains(x.Type));
        }
        if (filters.AccountId is { } accountId)
        {
            rows = rows.Where(x => x.AccountId == accountId || x.TargetAccountId == accountId);
        }
        if (filters.BudgetId is not null || filters.FundId is not null)
        {
            rows = InFundScope(rows, filters.BudgetId, filters.FundId);
        }
        if (filters.Search is { } search)
        {
            var pattern = $"%{EscapeLikePattern(search)}%";
            rows = rows.Where(x =>
                EF.Functions.ILike(x.Title!, pattern, LikeEscape)
                || EF.Functions.ILike(x.AccountName!, pattern, LikeEscape)
                || EF.Functions.ILike(x.TargetAccountName!, pattern, LikeEscape)
                || EF.Functions.ILike(x.BudgetName!, pattern, LikeEscape)
                || EF.Functions.ILike(x.FundName!, pattern, LikeEscape));
        }

        return rows;
    }

    /// <summary>
    /// The rows with a fund transaction in the scope: incomes allocated into it, expenses paid from it and allocations
    /// that have a line in it.
    /// </summary>
    private IQueryable<FeedRow> InFundScope(IQueryable<FeedRow> rows, Guid? budgetId, Guid? fundId)
    {
        var linked = dbContext.FundTransactions.AsQueryable();
        if (budgetId is not null)
        {
            linked = linked.Where(f => f.Fund.BudgetId == budgetId);
        }
        if (fundId is not null)
        {
            linked = linked.Where(f => f.FundId == fundId);
        }

        return rows.Where(x => linked.Any(f => f.AccountTransactionId == x.Id || f.AllocationId == x.Id));
    }

    private const string LikeEscape = @"\";

    private static string EscapeLikePattern(string value)
        => value.Replace(LikeEscape, LikeEscape + LikeEscape).Replace("%", LikeEscape + "%").Replace("_", LikeEscape + "_");

    /// <summary>
    /// Every item of the ledger as a flat row. The type codes are <see cref="FeedItemType"/>
    /// (0 Income, 1 Expense, 2 Transfer, 3 Exchange, 4 Allocation), and the account transaction types are
    /// <see cref="AccountTransactionType"/> (0 Regular, 1 InitialBalance). Zero amounts are left out.
    /// An expense paid from a fund carries the fund and its budget. An allocation is one row per Allocation (its total, the
    /// distinct budget and fund names of its lines); its last column marks an initial allocation like an initial balance.
    /// The ledger parameter is named explicitly: the default names of interpolated parameters (p0, p1…) collide with
    /// the parameters EF generates for the composed query, such as the paging ones.
    /// </summary>
    private IQueryable<FeedRow> Rows(Guid ledgerId)
        => dbContext.Database.SqlQueryRaw<FeedRow>(RowsSql, new NpgsqlParameter("feed_ledger_id", ledgerId));

    private const string RowsSql = """
        SELECT t."Id",
               CASE WHEN t."Amount" > 0 THEN 0 ELSE 1 END AS "Type",
               t."Date",
               t."CreatedAt",
               t."Title",
               t."AccountId",
               a."Name" AS "AccountName",
               NULL::uuid AS "TargetAccountId",
               NULL::text AS "TargetAccountName",
               cf."BudgetId" AS "BudgetId",
               cb."Name" AS "BudgetName",
               c."FundId" AS "FundId",
               cf."Name" AS "FundName",
               t."Amount",
               t."Currency",
               NULL::numeric AS "ReceivedAmount",
               NULL::text AS "ReceivedCurrency",
               t."Type" = 1 AS "IsInitialBalance"
        FROM "AccountTransactions" t
        JOIN "Accounts" a ON a."Id" = t."AccountId"
        LEFT JOIN "FundTransactions" c ON c."AccountTransactionId" = t."Id" AND t."Amount" < 0
        LEFT JOIN "Funds" cf ON cf."Id" = c."FundId"
        LEFT JOIN "Budgets" cb ON cb."Id" = cf."BudgetId"
        WHERE a."LedgerId" = @feed_ledger_id AND t."Type" IN (0, 1) AND t."Amount" <> 0
        UNION ALL
        SELECT tr."Id",
               CASE WHEN e."Currency" = i."Currency" THEN 2 ELSE 3 END,
               i."Date",
               tr."CreatedAt",
               i."Title",
               e."AccountId",
               ea."Name",
               i."AccountId",
               ia."Name",
               NULL, NULL, NULL, NULL,
               -e."Amount",
               e."Currency",
               CASE WHEN e."Currency" <> i."Currency" THEN i."Amount" END,
               CASE WHEN e."Currency" <> i."Currency" THEN i."Currency" END,
               FALSE
        FROM "AccountTransfers" tr
        JOIN "AccountTransactions" e ON e."Id" = tr."ExpenseId"
        JOIN "Accounts" ea ON ea."Id" = e."AccountId"
        JOIN "AccountTransactions" i ON i."Id" = tr."IncomeId"
        JOIN "Accounts" ia ON ia."Id" = i."AccountId"
        WHERE ea."LedgerId" = @feed_ledger_id
        UNION ALL
        SELECT al."Id",
               4,
               al."Date",
               al."CreatedAt",
               NULL::text,
               NULL, NULL, NULL, NULL,
               NULL::uuid,
               s."BudgetNames",
               NULL::uuid,
               s."FundNames",
               s."Amount",
               al."Currency",
               NULL, NULL,
               al."IsInitial"
        FROM "Allocations" al
        JOIN LATERAL (
            SELECT SUM(f."Amount") AS "Amount",
                   string_agg(DISTINCT b."Name", ', ') AS "BudgetNames",
                   string_agg(DISTINCT fu."Name", ', ') AS "FundNames"
            FROM "FundTransactions" f
            JOIN "Funds" fu ON fu."Id" = f."FundId"
            JOIN "Budgets" b ON b."Id" = fu."BudgetId"
            WHERE f."AllocationId" = al."Id") s ON TRUE
        WHERE al."LedgerId" = @feed_ledger_id AND s."Amount" <> 0
        """;

    /// <summary>
    /// The common shape of every feed row; the column names match the property names.
    /// </summary>
    private sealed class FeedRow
    {
        public Guid Id { get; set; }
        public int Type { get; set; }
        public DateTimeOffset Date { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string? Title { get; set; }
        public Guid? AccountId { get; set; }
        public string? AccountName { get; set; }
        public Guid? TargetAccountId { get; set; }
        public string? TargetAccountName { get; set; }
        public Guid? BudgetId { get; set; }
        public string? BudgetName { get; set; }
        public Guid? FundId { get; set; }
        public string? FundName { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = null!;
        public decimal? ReceivedAmount { get; set; }
        public string? ReceivedCurrency { get; set; }
        public bool IsInitialBalance { get; set; }
    }
}
