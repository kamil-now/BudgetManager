using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Models;

namespace BudgetManager.Domain.Services;

/// <summary>
/// Splits an income between funds by the steps of an allocation template, a pipeline: the steps run in
/// <see cref="AllocationRule.Sequence"/> order on what the steps before them left. A fixed step takes its amount,
/// a percent step takes its percent of what is left, rounded towards zero to cents. Whatever is left after the last step
/// stays unallocated, and steps that take nothing are left out. An income below <see cref="Minimum"/> cannot fund
/// every fixed step and is refused.
/// </summary>
public static class AllocationCalculator
{
    private const decimal Whole = 10_000m; // 100% in hundredths of a percent
    private const decimal CentsCap = Constants.MaxMoneyAmount * 100 + 1; // one cent more than any valid amount

    /// <summary>
    /// The smallest income that funds every fixed step fully, worked out backwards in cents: a fixed step adds its amount
    /// to what is needed, a percent step that needs <c>n</c> cents after it needs the least <c>x</c> with
    /// <c>x - floor(x * p) >= n</c> before it. A template of percent steps only has no minimum (0). The result is capped
    /// at one cent above <see cref="Constants.MaxMoneyAmount"/>.
    /// </summary>
    public static decimal Minimum(IEnumerable<AllocationRule> rules)
    {
        var need = 0m; // cents
        foreach (var rule in rules.OrderByDescending(x => x.Sequence))
        {
            if (rule.Type == AllocationType.Fixed)
            {
                need += rule.Value * 100;
            }
            else if (need > 0)
            {
                var percent = rule.Value * 100;
                need = percent >= Whole ? CentsCap : decimal.Floor((need - 1) * Whole / (Whole - percent)) + 1;
            }

            need = Math.Min(need, CentsCap);
        }

        return need / 100;
    }

    /// <summary>
    /// What each step of a budget's allocation template gives its fund.
    /// </summary>
    public static IReadOnlyList<FundAmount> Allocate(decimal income, IEnumerable<AllocationRule> rules)
        => [.. Split(income, rules).Select(x => new FundAmount(x.TargetId, x.Amount))];

    /// <summary>
    /// What each step gives its target, in sequence order.
    /// </summary>
    public static IReadOnlyList<AllocationShare> Split(decimal income, IEnumerable<AllocationRule> rules)
    {
        if (income <= 0)
        {
            return [];
        }

        var ordered = rules.OrderBy(x => x.Sequence).ToArray();
        if (income < Minimum(ordered))
        {
            throw new ArgumentOutOfRangeException(nameof(income), income, "Income is below the allocation's minimum.");
        }

        var remaining = income;
        var shares = new List<AllocationShare>(ordered.Length);
        foreach (var rule in ordered)
        {
            var amount = rule.Type == AllocationType.Fixed
                ? rule.Value
                : decimal.Round(remaining * rule.Value / 100, Constants.MoneyDecimalPlaces, MidpointRounding.ToZero);
            remaining -= amount;

            if (amount > 0)
            {
                shares.Add(new AllocationShare(rule.TargetId, amount));
            }
        }

        return shares;
    }
}
