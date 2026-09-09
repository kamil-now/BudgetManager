using System.Security.Claims;
using BudgetManager.Application.Services;
using BudgetManager.Application.Validators;

namespace BudgetManager.Api.Services;

public class HttpContextUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public string? Id => httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    public string? Email => httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;
    public string? Name => httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value;

    // The token is signed, so the id it carries is trusted without reading the database.
    public Guid UserId
    {
        get
        {
            if (!Guid.TryParse(Id, out var userId) || userId == Guid.Empty)
            {
                throw new AuthenticationException($"User ID '{userId}' is invalid.");
            }
            return userId;
        }
    }
}
