using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Interfaces;

public interface IUserPreferencesReader
{
    /// <summary>
    /// The preferences of the user, or null when there is no such user.
    /// </summary>
    Task<UserPreferences?> ReadAsync(Guid userId, CancellationToken cancellationToken = default);
}
