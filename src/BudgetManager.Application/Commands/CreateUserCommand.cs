namespace BudgetManager.Application.Commands;

public record CreateUserCommand(
  string Name,
  string Email,
  string HashedPassword
): IRequest<Guid>;
