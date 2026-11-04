using BudgetManager.Application.Commands;
using BudgetManager.Application.Interfaces;
using BudgetManager.Application.Models;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class LoginHandler(IEntityStore store, IPasswordHasher passwordHasher) : IRequestHandler<LoginCommand, UserDTO>
{
    private const string InvalidCredentialsMessage = "Invalid email or password.";

    public async Task<UserDTO> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var email = command.Email.NormalizeEmail();
        var users = await store.GetAsync<User>(x => x.Email == email, cancellationToken);
        if (users == null || !users.Any())
        {
            // hash anyway so an unknown email takes as long as a wrong password
            passwordHasher.Hash(command.Password);
            throw new AuthenticationException(InvalidCredentialsMessage);
        }
        if (users.Count() > 1)
        {
            throw new ValidationException($"Multiple users found with email {email}. Please contact support.");
        }

        var user = users.First();

        if (!passwordHasher.Verify(command.Password, user.HashedPassword))
        {
            throw new AuthenticationException(InvalidCredentialsMessage);
        }

        return new UserDTO(user);
    }
}
