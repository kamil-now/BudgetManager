using BudgetManager.Application.Models;
using BudgetManager.Application.Security;

namespace BudgetManager.Application.Commands;

public record CreateUserCommand(
  string Email,
  string Password,
  string? Name
) : IRequest<UserDTO>, IAnonymousRequest;
