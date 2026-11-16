using BudgetManager.Application.Models;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;
using BudgetManager.Domain.Models;
using BudgetManager.Domain.Services;

namespace BudgetManager.Application.Handlers;

internal static class IncomeAllocation
{
    /// <summary>
    /// The amount each step gives a fund from <paramref name="income"/>: the pipeline of the template, of the custom
    /// lines, or of the ledger template followed by the budget templates. Every template (which must be in the income's
    /// currency) and every fund must belong to <paramref name="ledgerId"/>, custom lines must allocate something, and the
    /// income must reach the minimum of the steps.
    /// </summary>
    public static async Task<IReadOnlyList<FundAmount>> SplitAsync(
        IEntityStore store,
        AllocationInput allocation,
        Money income,
        Guid ledgerId,
        CancellationToken cancellationToken)
    {
        if (allocation.TemplateId is { } templateId)
        {
            return await FromTemplateAsync(store, templateId, income, ledgerId, cancellationToken);
        }
        if (allocation.LedgerTemplate is { } ledgerTemplate)
        {
            return await FromLedgerTemplateAsync(store, ledgerTemplate, income, ledgerId, cancellationToken);
        }
        return await FromLinesAsync(store, allocation.Lines ?? [], income, ledgerId, cancellationToken);
    }

    public static IEnumerable<FundTransaction> ToFundTransactions(this IEnumerable<FundAmount> amounts, AccountTransaction income)
        => amounts.Select(x => FundTransaction.Allocate(x.FundId, new Money(x.Amount, income.Value.Currency), income));

    private static async Task<IReadOnlyList<FundAmount>> FromTemplateAsync(IEntityStore store, Guid templateId, Money income, Guid ledgerId, CancellationToken cancellationToken)
    {
        if (!await store.ExistsAsync<AllocationTemplate>(x => x.Id == templateId && x.Budget.LedgerId == ledgerId, cancellationToken))
        {
            throw new ValidationException("Allocation template must belong to the account's ledger.");
        }

        var template = await store.GetAsync<AllocationTemplate>(templateId, [x => x.Lines], cancellationToken);

        if (template.Currency != income.Currency)
        {
            throw new ValidationException($"Allocation template is in {template.Currency}, the amount is in {income.Currency}.");
        }

        return Split(income, template.ToRules());
    }

    /// <summary>
    /// Splits <paramref name="income"/> between budgets by the ledger template, then each budget's share between its funds
    /// by the one picked budget template of that budget. A budget with several lines gets one share, their sum.
    /// </summary>
    private static async Task<IReadOnlyList<FundAmount>> FromLedgerTemplateAsync(
        IEntityStore store,
        LedgerAllocationInput input,
        Money income,
        Guid ledgerId,
        CancellationToken cancellationToken)
    {
        if (!await store.ExistsAsync<LedgerAllocationTemplate>(x => x.Id == input.TemplateId && x.LedgerId == ledgerId, cancellationToken))
        {
            throw new ValidationException("Ledger allocation template must belong to the account's ledger.");
        }

        var ledgerTemplate = await store.GetAsync<LedgerAllocationTemplate>(input.TemplateId, [x => x.Lines], cancellationToken);
        if (ledgerTemplate.Currency != income.Currency)
        {
            throw new ValidationException($"Ledger allocation template is in {ledgerTemplate.Currency}, the amount is in {income.Currency}.");
        }

        var budgetTemplates = await PickBudgetTemplatesAsync(store, input.BudgetTemplateIds ?? [], ledgerTemplate, income, cancellationToken);

        EnsureReachesMinimum(income, ledgerTemplate.ToRules());
        var budgetShares = AllocationCalculator.Split(income.Amount, ledgerTemplate.ToRules())
            .GroupBy(x => x.TargetId)
            .Select(g => (BudgetId: g.Key, Amount: g.Sum(x => x.Amount)));

        var amounts = new List<FundAmount>();
        foreach (var (budgetId, amount) in budgetShares)
        {
            var template = budgetTemplates[budgetId];
            var rules = template.ToRules();
            var minimum = AllocationCalculator.Minimum(rules);
            if (amount < minimum)
            {
                throw new ValidationException(FormattableString.Invariant(
                    $"Share {amount:0.##} {income.Currency} of allocation template {template.Name} is below its minimum of {minimum:0.##} {income.Currency}."));
            }

            amounts.AddRange(AllocationCalculator.Allocate(amount, rules));
        }

        return amounts;
    }

    /// <summary>
    /// The picked budget template of every budget of <paramref name="ledgerTemplate"/>, by budget: exactly one per budget,
    /// none for any other budget, all in the income's currency.
    /// </summary>
    private static async Task<Dictionary<Guid, AllocationTemplate>> PickBudgetTemplatesAsync(
        IEntityStore store,
        IEnumerable<Guid> budgetTemplateIds,
        LedgerAllocationTemplate ledgerTemplate,
        Money income,
        CancellationToken cancellationToken)
    {
        var templates = new List<AllocationTemplate>();
        foreach (var id in budgetTemplateIds.Distinct())
        {
            templates.Add(await store.GetAsync<AllocationTemplate>(id, [x => x.Lines], cancellationToken));
        }

        var budgetIds = ledgerTemplate.Lines.Select(x => x.BudgetId).ToHashSet();
        if (templates.Any(x => !budgetIds.Contains(x.BudgetId)))
        {
            throw new ValidationException("Budget allocation templates must belong to budgets of the ledger allocation template.");
        }
        if (templates.GroupBy(x => x.BudgetId).Any(g => g.Count() > 1))
        {
            throw new ValidationException("Only one allocation template can be picked per budget.");
        }
        if (templates.Count != budgetIds.Count)
        {
            throw new ValidationException("Every budget of the ledger allocation template needs an allocation template.");
        }
        if (templates.FirstOrDefault(x => x.Currency != income.Currency) is { } other)
        {
            throw new ValidationException($"Allocation template {other.Name} is in {other.Currency}, the amount is in {income.Currency}.");
        }

        return templates.ToDictionary(x => x.BudgetId);
    }

    private static void EnsureReachesMinimum(Money income, IReadOnlyList<AllocationRule> rules)
    {
        var minimum = AllocationCalculator.Minimum(rules);
        if (income.Amount < minimum)
        {
            throw new ValidationException(FormattableString.Invariant(
                $"Amount {income.Amount:0.##} {income.Currency} is below the allocation's minimum of {minimum:0.##} {income.Currency}."));
        }
    }

    private static IReadOnlyList<FundAmount> Split(Money income, IReadOnlyList<AllocationRule> rules)
    {
        EnsureReachesMinimum(income, rules);

        return AllocationCalculator.Allocate(income.Amount, rules);
    }

    private static async Task<IReadOnlyList<FundAmount>> FromLinesAsync(
        IEntityStore store,
        IEnumerable<AllocationLineInput> lines,
        Money income,
        Guid ledgerId,
        CancellationToken cancellationToken)
    {
        var rules = lines.Select(x => x.ToRule()).ToArray();
        var fundIds = rules.Select(x => x.TargetId).Distinct().ToArray();

        var funds = await store.GetAsync<Fund>(x => fundIds.Contains(x.Id) && x.Budget.LedgerId == ledgerId, cancellationToken);
        if (funds.Count() != fundIds.Length)
        {
            throw new ValidationException("Allocated funds must belong to a budget in the account's ledger.");
        }

        var amounts = Split(income, rules);
        if (amounts.Count == 0)
        {
            throw new ValidationException("Allocation does not allocate anything to funds.");
        }

        return amounts;
    }
}
