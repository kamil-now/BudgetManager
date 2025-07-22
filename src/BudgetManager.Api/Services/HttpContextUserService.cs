using System.Security.Claims;
using BudgetManager.Application.Services;

namespace BudgetManager.Api.Services;

public class HttpContextUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public string? Id => httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    public string? Email => httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;
    public string? Name => httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value;
}
