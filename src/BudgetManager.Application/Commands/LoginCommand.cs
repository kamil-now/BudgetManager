using BudgetManager.Application.Models;

namespace BudgetManager.Application.Commands;

public record LoginCommand(string Email, string Password): IRequest<UserDTO>;
