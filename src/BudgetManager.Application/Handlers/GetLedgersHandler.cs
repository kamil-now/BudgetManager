using BudgetManager.Application.Interfaces;
using BudgetManager.Application.Models;
using BudgetManager.Application.Queries;
using BudgetManager.Application.Services;

namespace BudgetManager.Application.Handlers;

public sealed class GetLedgersHandler(ICurrentUserService currentUser, ILedgerListReader ledgerListReader)
    : IRequestHandler<GetLedgersQuery, IReadOnlyList<LedgerListItemDTO>>
{
    public async Task<IReadOnlyList<LedgerListItemDTO>> Handle(GetLedgersQuery query, CancellationToken cancellationToken)
    {
        var ledgers = await ledgerListReader.ReadAllAsync(currentUser.UserId, cancellationToken);
        return [.. ledgers.Select(x => new LedgerListItemDTO { Id = x.Id, Name = x.Name, Description = x.Description })];
    }
}
