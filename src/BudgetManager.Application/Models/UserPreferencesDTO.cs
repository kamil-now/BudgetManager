namespace BudgetManager.Application.Models;

public record UserPreferencesDTO(
    string Theme,
    string DateFormat,
    string DefaultCurrency,
    IEnumerable<string> FavoriteCurrencies,
    string Language);
