using BudgetManager.Application.Interfaces;
using BudgetManager.Application.Models;
using BudgetManager.Application.Queries;
using BudgetManager.Application.Validators;

namespace BudgetManager.Application.Handlers;

public sealed class GetAccountHandler(IAccountReader accountReader) : IRequestHandler<GetAccountQuery, AccountDTO>
{
    public async Task<AccountDTO> Handle(GetAccountQuery query, CancellationToken cancellationToken)
    {
        var account = await accountReader.ReadAsync(query.Id, cancellationToken);
        if (account is null)
        {
            throw new NotFoundException($"Account {query.Id} not found.");
        }

        return new AccountDTO
        {
            Id = account.Id,
            LedgerId = account.LedgerId,
            Balance = account.Balance,
            Name = account.Name
        };
    }
}
