using BudgetManager.Application.Security;

namespace BudgetManager.Application.Commands;

/// <summary>
/// Deletes the current user and everything they own; the password confirms it.
/// </summary>
public record DeleteUserCommand(string Password) : IRequest, IRequiresAccess
{
    IEnumerable<Resource> IRequiresAccess.Resources => [];
}
