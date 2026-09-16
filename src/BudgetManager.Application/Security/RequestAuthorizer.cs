using BudgetManager.Application.Services;
using BudgetManager.Application.Validators;
using BudgetManager.Application.Interfaces;

namespace BudgetManager.Application.Security;

public sealed class RequestAuthorizer(ICurrentUserService currentUser, IResourceOwnerReader ownerReader) : IRequestAuthorizer
{
    public async Task AuthorizeAsync(object request, CancellationToken cancellationToken)
    {
        if (request is IAnonymousRequest)
        {
            return;
        }

        var userId = currentUser.UserId;

        if (request is not IRequiresAccess requiresAccess)
        {
            throw new InvalidOperationException($"{request.GetType().Name} declares neither {nameof(IRequiresAccess)} nor {nameof(IAnonymousRequest)}.");
        }

        foreach (var resource in requiresAccess.Resources)
        {
            await EnsureAccessibleAsync(resource, userId, cancellationToken);
        }
    }

    private async Task EnsureAccessibleAsync(Resource resource, Guid userId, CancellationToken cancellationToken)
    {
        // A missing resource is reported the same way as one owned by somebody else, so ids of other users' data stay unconfirmed.
        if (await resource.ReadOwnerIdAsync(ownerReader, cancellationToken) != userId)
        {
            throw new AuthorizationException($"{resource.EntityType.Name} with ID '{resource.Id}' cannot be accessed by user with ID '{userId}'.");
        }
    }
}
