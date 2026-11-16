using BudgetManager.Application.Services;
using BudgetManager.Application.Validators;

namespace BudgetManager.DemoDataInitializer;

public sealed class DemoUser : ICurrentUserService
{
    private Guid? userId;

    public string? Id => userId?.ToString();
    public string? Email => DemoDataInitializer.Email;
    public string? Name => DemoDataInitializer.UserName;

    public Guid UserId => userId ?? throw new AuthenticationException("The initializer is not signed in.");

    public void SignIn(Guid id) => userId = id;
}
