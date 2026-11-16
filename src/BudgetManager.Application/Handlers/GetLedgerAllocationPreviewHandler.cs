using BudgetManager.Application.Interfaces;
using BudgetManager.Application.Models;
using BudgetManager.Application.Queries;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Interfaces;
using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Handlers;

public sealed class GetLedgerAllocationPreviewHandler(IEntityStore store, IFundLabelReader fundLabelReader)
    : IRequestHandler<GetLedgerAllocationPreviewQuery, AllocationPreviewDTO>
{
    public async Task<AllocationPreviewDTO> Handle(GetLedgerAllocationPreviewQuery query, CancellationToken cancellationToken)
    {
        if (query.Amount <= 0)
        {
            throw new ValidationException("Amount must be positive.");
        }
        query.Amount.EnsureValidAmount();
        query.Currency.EnsureValidCurrency();
        if (query.Allocation is null)
        {
            throw new ValidationException("Allocation cannot be empty.");
        }

        var amount = new Money(query.Amount, query.Currency);
        query.Allocation.EnsureValidAllocation(amount);

        var amounts = await IncomeAllocation.SplitAsync(store, query.Allocation, amount, query.LedgerId, cancellationToken);
        var labels = await fundLabelReader.ReadAsync(amounts.Select(x => x.FundId), cancellationToken);

        return new AllocationPreviewDTO(
            [.. amounts.Select(x => new AllocationPreviewDTO.Line(x.FundId, labels[x.FundId].FundName, labels[x.FundId].BudgetName, x.Amount))],
            query.Amount - amounts.Sum(x => x.Amount));
    }
}
