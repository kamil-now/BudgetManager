using BudgetManager.Application.Commands;
using BudgetManager.Application.Services;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class CreateLedgerHandler(ICurrentUserService currentUser, IBudgetManagerService budgetService) : IRequestHandler<CreateLedgerCommand, Guid>
{
  public async Task<Guid> Handle(CreateLedgerCommand command, CancellationToken cancellationToken)
  {
    if (!Guid.TryParse(currentUser.Id, out var userId))
    {
      throw new InvalidOperationException($"Invalid user ID '{currentUser.Id}'.");

    }
    if (!await budgetService.ExistsAsync<User>(x => x.Id == userId, cancellationToken))
    {
      throw new InvalidOperationException($"User with ID '{userId}' does not exist.");
    }
    // TODO
    var entity = await budgetService.CreateAsync(new Ledger
    {
      OwnerId = userId,
      Name = command.Name,
      Description = command.Description,
    }, cancellationToken) ?? throw new InvalidOperationException("Failed to create ledger.");

    await budgetService.SaveChangesAsync(cancellationToken);

    if (entity.Id == Guid.Empty)
    {
      throw new InvalidOperationException("Ledger ID cannot be empty.");
    }
    return entity.Id;
  }
}
