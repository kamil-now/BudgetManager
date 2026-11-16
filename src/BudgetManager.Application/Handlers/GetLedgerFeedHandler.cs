using BudgetManager.Application.Interfaces;
using BudgetManager.Application.Models;
using BudgetManager.Application.Queries;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Enums;

namespace BudgetManager.Application.Handlers;

public sealed class GetLedgerFeedHandler(ILedgerFeedReader feedReader) : IRequestHandler<GetLedgerFeedQuery, LedgerFeedPageDTO>
{
    public async Task<LedgerFeedPageDTO> Handle(GetLedgerFeedQuery query, CancellationToken cancellationToken)
    {
        var filters = query.Filters ?? new();

        filters.From.EnsureValidRange(filters.To);
        filters.Offset.EnsureNonnegative(nameof(filters.Offset));
        if (filters.Limit < 1 || filters.Limit > Constants.MaxFeedLimit)
        {
            throw new ValidationException($"Limit must be between 1 and {Constants.MaxFeedLimit}.");
        }
        filters.Search?.EnsureNotLongerThan(Constants.MaxSearchLength, nameof(filters.Search));
        foreach (var type in filters.Types ?? [])
        {
            if (!Enum.IsDefined(type))
            {
                throw new ValidationException($"Type '{type}' is not valid.");
            }
        }

        var feed = await feedReader.ReadAsync(
            query.LedgerId,
            filters with { Search = string.IsNullOrWhiteSpace(filters.Search) ? null : filters.Search.Trim() },
            cancellationToken);

        return new LedgerFeedPageDTO(
            [.. feed.Items.Select(x => new FeedItemDTO(
                x.Id,
                x.Type,
                x.Date,
                x.Title,
                x.AccountId,
                x.AccountName,
                x.TargetAccountId,
                x.TargetAccountName,
                x.BudgetId,
                x.BudgetName,
                x.FundId,
                x.FundName,
                x.Value,
                x.Received,
                x.IsInitialBalance,
                x.Type == FeedItemType.Income ? x.Allocations.Sum(a => a.Amount.Amount) : 0,
                [.. x.Allocations.Select(a => new FeedAllocationDTO(a.Id, a.FundId, a.FundName, a.BudgetId, a.BudgetName, a.Amount))]))],
            feed.Total,
            filters.Offset,
            filters.Limit);
    }
}
