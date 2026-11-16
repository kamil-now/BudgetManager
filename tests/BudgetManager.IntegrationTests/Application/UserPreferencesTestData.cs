using BudgetManager.Application.Models;
using BudgetManager.Domain;

namespace BudgetManager.IntegrationTests.Application;

public static class UserPreferencesTestData
{
    public static readonly UserPreferencesDTO Valid = new("dark", "dd.MM.yyyy", "PLN", ["PLN", "EUR"], "pl");

    public sealed class Invalid : TheoryData<UserPreferencesDTO, string>
    {
        public Invalid()
        {
            Add(Valid with { Theme = "blue" }, "Theme value 'blue' is not valid. Allowed values are: system, light, dark.");
            Add(Valid with { DateFormat = "yyyy/MM/dd" }, "DateFormat value 'yyyy/MM/dd' is not valid. Allowed values are: yyyy-MM-dd, dd.MM.yyyy, dd/MM/yyyy, MM/dd/yyyy.");
            Add(Valid with { DefaultCurrency = "eur" }, "DefaultCurrency value 'eur' is not a valid currency code.");
            Add(Valid with { FavoriteCurrencies = null! }, "FavoriteCurrencies cannot be empty.");
            Add(Valid with { FavoriteCurrencies = ["EUR", "US"] }, "FavoriteCurrencies currency value 'US' is not a valid currency code.");
            Add(Valid with { FavoriteCurrencies = ["EUR", "EUR"] }, "FavoriteCurrencies must be unique.");
            Add(Valid with { FavoriteCurrencies = ["AAA", "BBB", "CCC", "DDD", "EEE", "FFF", "GGG", "HHH", "III", "JJJ", "KKK"] },
                $"FavoriteCurrencies cannot have more than {Constants.MaxFavoriteCurrencies} entries.");
            Add(Valid with { Language = "de" }, "Language value 'de' is not valid. Allowed values are: en, pl.");
        }
    }
}
