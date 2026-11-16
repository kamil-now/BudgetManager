using BudgetManager.Application.Commands;
using BudgetManager.Application.Interfaces;
using BudgetManager.Application.Services;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class DeleteUserHandler(ICurrentUserService currentUser, IEntityStore store, IPasswordHasher passwordHasher) : IRequestHandler<DeleteUserCommand>
{
    public async Task Handle(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        if (string.IsNullOrEmpty(command.Password))
        {
            throw new ValidationException("Password cannot be empty.");
        }

        var user = await store.GetAsync<User>(userId, cancellationToken);
        if (!passwordHasher.Verify(command.Password, user.HashedPassword))
        {
            // not an AuthenticationException: that one signs the user out
            throw new ValidationException("Password is incorrect.");
        }

        await store.RunInTransactionAsync(() => store.DeleteUserAsync(userId, cancellationToken), cancellationToken);
    }
}
