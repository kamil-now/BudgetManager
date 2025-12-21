using BudgetManager.Application.Commands;
using BudgetManager.Application.Interfaces;
using BudgetManager.Application.Models;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class CreateUserHandler(IBudgetManagerService budgetService, IPasswordHasher passwordHasher) : IRequestHandler<CreateUserCommand, UserDTO>
{
    public async Task<UserDTO> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        await ValidateCommandAsync(command, cancellationToken);

        var entity = await budgetService.CreateAsync(new User
        {
            Name = command.Name,
            Email = command.Email,
            HashedPassword = passwordHasher.Hash(command.Password),
        }, cancellationToken) ?? throw new InvalidOperationException("Failed to create user.");

        await budgetService.SaveChangesAsync(cancellationToken);
        var user = await budgetService.GetAsync<User>(x => x.Id == entity.Id, cancellationToken);
        if (entity.Id == Guid.Empty)
        {
            throw new InvalidOperationException("User ID cannot be empty.");
        }

        return new UserDTO(entity);
    }

    private async Task ValidateCommandAsync(CreateUserCommand command, CancellationToken cancellationToken)
    {
        command.Email.EnsureNotEmpty().EnsureNotLongerThan(Constants.MaxEmailLength);
        command.Name?.EnsureNotEmpty().EnsureNotLongerThan(Constants.MaxNameLength);
        command.Password.EnsureNotEmpty();

        // TODO evaluate password strenght

        if (await budgetService.ExistsAsync<User>(x => x.Email == command.Email, cancellationToken))
        {
            throw new ValidationException($"User with email '{command.Email}' already exists.");
        }
    }
}
