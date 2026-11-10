using BudgetManager.Application.Interfaces;
using BudgetManager.Application.Models;
using BudgetManager.Application.Queries;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Handlers;

public sealed class GetBudgetHandler(IBudgetReader budgetReader) : IRequestHandler<GetBudgetQuery, BudgetDTO>
{
    public async Task<BudgetDTO> Handle(GetBudgetQuery query, CancellationToken cancellationToken)
    {
        var budget = await budgetReader.ReadAsync(query.Id, cancellationToken) ?? throw new NotFoundException($"Budget {query.Id} not found.");
        var funds = budget.Funds.Select(fund => new BudgetDTO.Fund
        {
            Id = fund.Id,
            Balance = fund.Balance,
            Name = fund.Name,
            Description = fund.Description
        }).ToArray();

        return new BudgetDTO
        {
            Id = budget.Id,
            Name = budget.Name,
            Description = budget.Description,
            Funds = funds,
            Balance = Balance.Sum(funds.Select(x => x.Balance))
        };
    }
}
