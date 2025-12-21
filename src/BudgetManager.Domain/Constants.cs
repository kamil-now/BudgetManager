namespace BudgetManager.Domain;

public static class Constants
{
    public const string EmailRegexp = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    public const int HashedPasswordLength = 60;
    public const int CurrencyCodeLength = 3;
    public const int MaxNameLength = 100;
    public const int MaxEmailLength = 254;  // RFC 5321 limit
    public const int MaxTitleLength = 200;
    public const int MaxCommentLength = 1000;
    public const int MaxTagsLength = 500;
}
