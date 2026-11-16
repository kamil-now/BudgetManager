namespace BudgetManager.Domain;

public static class Constants
{
    public const string InitialBalanceTransactionTitle = "Initial balance";
    public const string EmailRegexp = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    public const int HashedPasswordLength = 60;
    public const int CurrencyCodeLength = 3;
    public const int MoneyPrecision = 18;
    public const int MoneyDecimalPlaces = 2;
    public const decimal MaxMoneyAmount = 9_999_999_999_999_999.99m;
    public const int PercentPrecision = 5;
    public const int PercentDecimalPlaces = 2;
    public const int MaxAllocationPercent = 100;
    public const int MaxNameLength = 100;
    public const int MaxEmailLength = 254;  // RFC 5321 limit
    public const int MinPasswordLength = 8;
    public const int MaxPasswordBytes = 72; // bcrypt ignores everything past 72 bytes
    public const int MaxTitleLength = 200;
    public const int MaxSearchLength = 100;
    public const int DefaultFeedLimit = 50;
    public const int MaxFeedLimit = 200;

    public const int MaxPreferenceValueLength = 10;
    public const string DefaultTheme = "system";
    public static readonly IReadOnlyList<string> Themes = ["system", "light", "dark"];
    public const string DefaultDateFormat = "yyyy-MM-dd";
    public static readonly IReadOnlyList<string> DateFormats = ["yyyy-MM-dd", "dd.MM.yyyy", "dd/MM/yyyy", "MM/dd/yyyy"];
    public const string DefaultCurrency = "EUR";
    public static readonly IReadOnlyList<string> DefaultFavoriteCurrencies = ["EUR", "USD", "PLN"];
    public const int MaxFavoriteCurrencies = 10;
    public const int MaxFavoriteCurrenciesLength = MaxFavoriteCurrencies * (CurrencyCodeLength + 1) - 1; // comma-joined
    public const string DefaultLanguage = "en";
    public static readonly IReadOnlyList<string> Languages = ["en", "pl"];
}
