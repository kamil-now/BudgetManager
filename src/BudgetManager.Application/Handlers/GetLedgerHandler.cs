using BudgetManager.Application.Models;
using BudgetManager.Application.Queries;
using BudgetManager.Common.Models;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class GetLedgerHandler(IBudgetManagerService budgetService) : IRequestHandler<GetLedgerQuery, LedgerDTO?>
{
    public async Task<LedgerDTO?> Handle(GetLedgerQuery query, CancellationToken cancellationToken)
    {
        var ledger = await budgetService.GetLedgerAsync(x => x.Id == query.Id, cancellationToken);
        if (ledger is null)
        {
            return null;
        }

        var accounts = ledger.Accounts.Select(x => new LedgerDTO.Account()
        {
            Balance = x.GetBalance(),
            Name = x.Name,
            Description = x.Description
        }).ToArray();

        var budgets = ledger.Budgets.Select(budget =>
        {
            var funds = budget.Funds.Select(fund => new LedgerDTO.Fund()
            {
                Balance = fund.GetBalance(),
                Name = fund.Name,
                Description = fund.Description
            });
            return new LedgerDTO.Budget()
            {
                Funds = funds,
                Balance = Balance.Sum(funds.Select(x => x.Balance)),
                Name = budget.Name,
                Description = budget.Description,
            };
        });

        return new LedgerDTO()
        {
            Name = ledger.Name,
            Description = ledger.Description,
            Accounts = accounts,
            Budgets = budgets,
            Balance = Balance.Sum(accounts.Select(x => x.Balance))
        };
    }
}
