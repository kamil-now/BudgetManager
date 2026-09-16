using BudgetManager.Application.Interfaces;
using BudgetManager.Application.Models;
using BudgetManager.Application.Queries;
using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Handlers;

public sealed class GetLedgerHandler(ILedgerReader ledgerReader) : IRequestHandler<GetLedgerQuery, LedgerDTO?>
{
    public async Task<LedgerDTO?> Handle(GetLedgerQuery query, CancellationToken cancellationToken)
    {
        var ledger = await ledgerReader.ReadAsync(query.Id, cancellationToken);
        if (ledger is null)
        {
            return null;
        }

        var accounts = ledger.Accounts.Select(account => new LedgerDTO.Account()
        {
            Id = account.Id,
            Balance = account.Balance,
            Name = account.Name,
            Description = account.Description
        }).ToArray();

        var budgets = ledger.Budgets.Select(budget =>
        {
            var funds = budget.Funds.Select(fund => new LedgerDTO.Fund()
            {
                Id = fund.Id,
                Balance = fund.Balance,
                Name = fund.Name,
                Description = fund.Description
            }).ToArray();
            return new LedgerDTO.Budget()
            {
                Id = budget.Id,
                Funds = funds,
                Balance = Balance.Sum(funds.Select(x => x.Balance)),
                Name = budget.Name,
                Description = budget.Description,
            };
        }).ToArray();

        return new LedgerDTO()
        {
            Id = ledger.Id,
            Name = ledger.Name,
            Description = ledger.Description,
            Accounts = accounts,
            Budgets = budgets,
            Balance = Balance.Sum(accounts.Select(x => x.Balance))
        };
    }
}
