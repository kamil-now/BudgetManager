using BudgetManager.Application.Security;
using BudgetManager.Domain.Entities;

namespace BudgetManager.Application.Models;

/// <summary>
/// How an income is split between funds, by exactly one of: the allocation template <see cref="TemplateId"/> of one of
/// the ledger's budgets (its currency must be the income's), custom <see cref="Lines"/>, which follow the same pipeline
/// rules as a template, or a <see cref="LedgerTemplate"/>, which splits the income between budgets first. On an update,
/// empty <see cref="Lines"/> only remove the existing allocations.
/// </summary>
public record AllocationInput(
    Guid? TemplateId = null,
    IEnumerable<AllocationLineInput>? Lines = null,
    LedgerAllocationInput? LedgerTemplate = null)
{
    /// <summary>
    /// The templates and the funds the allocation touches, for the request that carries it to declare.
    /// </summary>
    public IEnumerable<Resource> ToResources()
    {
        if (TemplateId is { } templateId)
        {
            yield return Resource.Of<AllocationTemplate>(templateId);
        }
        foreach (var fundId in (Lines ?? []).Where(x => x is not null).Select(x => x.FundId).Distinct())
        {
            yield return Resource.Of<Fund>(fundId);
        }
        if (LedgerTemplate is not null)
        {
            yield return Resource.Of<LedgerAllocationTemplate>(LedgerTemplate.TemplateId);
            foreach (var budgetTemplateId in (LedgerTemplate.BudgetTemplateIds ?? []).Distinct())
            {
                yield return Resource.Of<AllocationTemplate>(budgetTemplateId);
            }
        }
    }

    /// <summary>
    /// Whether the allocation allocates anything: false only for the empty lines that clear an update's allocations.
    /// </summary>
    public bool AllocatesAnything => TemplateId is not null || LedgerTemplate is not null || Lines?.Any() == true;
}

/// <summary>
/// The ledger's allocation template <see cref="TemplateId"/> splits the income between budgets, then each budget's share
/// is split between its funds by the template of <see cref="BudgetTemplateIds"/> that belongs to that budget. Every
/// budget of the ledger template needs exactly one of them, all in the income's currency.
/// </summary>
public record LedgerAllocationInput(Guid TemplateId, IEnumerable<Guid> BudgetTemplateIds);
