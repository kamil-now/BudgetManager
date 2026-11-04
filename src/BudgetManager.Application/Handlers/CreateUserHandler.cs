using BudgetManager.Application.Commands;
using BudgetManager.Application.Interfaces;
using BudgetManager.Application.Models;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class CreateUserHandler(IEntityStore store, IPasswordHasher passwordHasher) : IRequestHandler<CreateUserCommand, UserDTO>
{
    public async Task<UserDTO> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var email = command.Email.NormalizeEmail();
        await ValidateCommandAsync(command, email, cancellationToken);

        var entity = await store.CreateAsync(new User
        {
            Name = command.Name,
            Email = email,
            HashedPassword = passwordHasher.Hash(command.Password),
        }, cancellationToken) ?? throw new InvalidOperationException("Failed to create user.");

        await store.SaveChangesAsync(cancellationToken);
        if (entity.Id == Guid.Empty)
        {
            throw new InvalidOperationException("User ID cannot be empty.");
        }

        return new UserDTO(entity);
    }

    private async Task ValidateCommandAsync(CreateUserCommand command, string email, CancellationToken cancellationToken)
    {
        email.EnsureValidEmail(nameof(command.Email));
        command.Name?.EnsureNotEmpty().EnsureNotLongerThan(Constants.MaxNameLength);
        command.Password.EnsureValidPassword();

        if (await store.ExistsAsync<User>(x => x.Email == email, cancellationToken))
        {
            throw new ConflictException($"User with email '{email}' already exists.");
        }
    }
}
