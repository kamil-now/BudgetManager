using BudgetManager.Application.Interfaces;
using BudgetManager.Application.Models;
using BudgetManager.Application.Queries;
using BudgetManager.Application.Services;
using BudgetManager.Application.Validators;

namespace BudgetManager.Application.Handlers;

public sealed class GetUserPreferencesHandler(ICurrentUserService currentUser, IUserPreferencesReader preferencesReader)
    : IRequestHandler<GetUserPreferencesQuery, UserPreferencesDTO>
{
    public async Task<UserPreferencesDTO> Handle(GetUserPreferencesQuery query, CancellationToken cancellationToken)
    {
        var preferences = await preferencesReader.ReadAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        return new UserPreferencesDTO(
            preferences.Theme,
            preferences.DateFormat,
            preferences.DefaultCurrency,
            preferences.FavoriteCurrencies,
            preferences.Language);
    }
}
