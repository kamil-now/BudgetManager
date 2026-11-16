using BudgetManager.Application.Interfaces;
using BudgetManager.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BudgetManager.Infrastructure.Persistence.Readers;

public sealed class UserPreferencesReader(ApplicationDbContext dbContext) : IUserPreferencesReader
{
    public async Task<UserPreferences?> ReadAsync(Guid userId, CancellationToken cancellationToken = default)
        => await dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => x.Preferences)
            .SingleOrDefaultAsync(cancellationToken);
}
