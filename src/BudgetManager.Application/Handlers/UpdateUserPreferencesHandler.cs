using BudgetManager.Application.Commands;
using BudgetManager.Application.Models;
using BudgetManager.Application.Services;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class UpdateUserPreferencesHandler(ICurrentUserService currentUser, IEntityStore store) : IRequestHandler<UpdateUserPreferencesCommand>
{
    public async Task Handle(UpdateUserPreferencesCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var preferences = new UserPreferencesDTO(command.Theme, command.DateFormat, command.DefaultCurrency, command.FavoriteCurrencies, command.Language)
            .EnsureValidPreferences();

        var user = await store.GetAsync<User>(userId, cancellationToken);

        user.Preferences.Theme = preferences.Theme;
        user.Preferences.DateFormat = preferences.DateFormat;
        user.Preferences.DefaultCurrency = preferences.DefaultCurrency;
        user.Preferences.FavoriteCurrencies = preferences.FavoriteCurrencies;
        user.Preferences.Language = preferences.Language;

        await store.SaveChangesAsync(cancellationToken);
    }
}
