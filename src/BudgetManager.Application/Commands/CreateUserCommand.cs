using BudgetManager.Application.Models;

namespace BudgetManager.Application.Commands;

public record CreateUserCommand(
  string Email,
  string Password,
  string? Name
) : IRequest<UserDTO>;
