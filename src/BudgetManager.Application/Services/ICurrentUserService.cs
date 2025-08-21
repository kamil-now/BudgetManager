namespace BudgetManager.Application.Services;

public interface ICurrentUserService
{
    public string? Id { get; }
    public string? Email { get; }
    public string? Name { get; }
}
