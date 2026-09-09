using BudgetManager.Application.Models;
using BudgetManager.Application.Security;

namespace BudgetManager.Application.Commands;

public record LoginCommand(string Email, string Password) : IRequest<UserDTO>, IAnonymousRequest;
