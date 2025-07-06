using BudgetManager.Application.Commands;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class CreateUserHandler(IBudgetManagerService budgetService) : IRequestHandler<CreateUserCommand, Guid>
{
  public async Task<Guid> Handle(CreateUserCommand command, CancellationToken cancellationToken)
  {
    if (await budgetService.ExistsAsync<User>(u => u.Email == command.Email, cancellationToken))
    {
      throw new InvalidOperationException($"User with email {command.Email} already exists.");
    }
    var entity = await budgetService.AddAsync(new User
    {
      Name = command.Name,
      Email = command.Email,
      HashedPassword = command.HashedPassword,
    }, cancellationToken) ?? throw new InvalidOperationException("Failed to create user.");
    if (entity.Id == Guid.Empty)
    {
      throw new InvalidOperationException("User ID cannot be empty.");
    }
    return entity.Id;
  }
}
