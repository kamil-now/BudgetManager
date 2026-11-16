namespace BudgetManager.Domain.Models;

/// <summary>
/// How the UI is set up for a user; stored as columns of the user's row.
/// </summary>
public sealed class UserPreferences
{
    public string Theme { get; set; } = Constants.DefaultTheme;
    public string DateFormat { get; set; } = Constants.DefaultDateFormat;
    public string DefaultCurrency { get; set; } = Constants.DefaultCurrency;
    /// <summary>
    /// The currencies offered first when choosing one; stored comma-joined.
    /// </summary>
    public List<string> FavoriteCurrencies { get; set; } = [.. Constants.DefaultFavoriteCurrencies];
    public string Language { get; set; } = Constants.DefaultLanguage;
}
