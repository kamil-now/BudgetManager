using BudgetManager.Application.Commands;
using BudgetManager.Application.Interfaces;
using BudgetManager.Application.Models;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class LoginHandler(IBudgetManagerService budgetService, IPasswordHasher passwordHasher) : IRequestHandler<LoginCommand, UserDTO>
{
    public async Task<UserDTO> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var users = await budgetService.GetAsync<User>(x => x.Email == command.Email, cancellationToken);
        if (users == null || !users.Any())
        {
            throw new UnauthorizedAccessException($"User with email {command.Email} not found.");
        }
        if (users.Count() > 1)
        {
            throw new InvalidOperationException($"Multiple users found with email {command.Email}. Please contact support.");
        }

        var user = users.First();

        if (!passwordHasher.Verify(command.Password, user.HashedPassword))
        {
            throw new UnauthorizedAccessException("Invalid password.");
        }

        return new UserDTO(user);
    }
}
