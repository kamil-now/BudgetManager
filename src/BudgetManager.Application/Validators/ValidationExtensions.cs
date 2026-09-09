using System.Runtime.CompilerServices;
using BudgetManager.Common.Models;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Validators;

public static class ValidationExtensions
{
    public static IEnumerable<string>? EnsureValidTags(this IEnumerable<string>? tags)
    {
        if (tags == null)
        {
            return tags;
        }
        if (tags.Any(tag => string.IsNullOrWhiteSpace(tag)))
        {
            throw new ValidationException("Each tag must have a value.");
        }
        if (string.Join(',', tags).Length > Constants.MaxTagsLength)
        {
            throw new ValidationException($"Tags are too long. Max combined tags length is {Constants.MaxTagsLength}.");
        }
        return tags;
    }

    public static IEnumerable<T> EnsureNotLongerThan<T>(this IEnumerable<T> val, int max, [CallerArgumentExpression(nameof(val))] string? paramName = null)
    {
        if (val.Count() > max)
        {
            throw new ValidationException($"{paramName?.TrimName()} value is too long. Max length is {max}.");
        }
        return val;
    }

    public static int EnsureNonnegative(this int val, [CallerArgumentExpression(nameof(val))] string? paramName = null)
     => (int)EnsureNonnegative((decimal)val, paramName);

    public static decimal EnsureNonnegative(this decimal val, [CallerArgumentExpression(nameof(val))] string? paramName = null)
    {
        if (val < 0)
        {
            throw new ValidationException($"{paramName?.TrimName()} must be greater than or equal zero.");
        }
        return val;
    }

    public static Guid EnsureNotEmpty(this Guid val, [CallerArgumentExpression(nameof(val))] string? paramName = null)
    {
        if (val == Guid.Empty)
        {
            throw new ValidationException($"{paramName?.TrimName()} cannot be empty.");
        }
        return val;
    }

    public static IEnumerable<T> EnsureNotEmpty<T>(this IEnumerable<T> val, [CallerArgumentExpression(nameof(val))] string? paramName = null)
    {
        if (!val.Any())
        {
            throw new ValidationException($"{paramName?.TrimName()} cannot be empty.");
        }
        return val;
    }

    public static Money EnsureValid(this Money val, [CallerArgumentExpression(nameof(val))] string? paramName = null)
    {
        if (val.Amount == 0)
        {
            throw new ValidationException($"{paramName?.TrimName()} amount cannot be zero.");
        }
        val.Currency.EnsureValidCurrency($"{paramName?.TrimName()} currency");
        val.Amount.EnsureValidAmount($"{paramName?.TrimName()} amount");

        return val;
    }

    public static decimal EnsureValidAmount(this decimal val, [CallerArgumentExpression(nameof(val))] string? paramName = null)
    {
        if (decimal.Round(val, Constants.MoneyDecimalPlaces) != val)
        {
            throw new ValidationException($"{paramName?.TrimName()} value '{val}' cannot have more than {Constants.MoneyDecimalPlaces} decimal places.");
        }
        return val;
    }

    public static string EnsureValidCurrency(this string val, [CallerArgumentExpression(nameof(val))] string? paramName = null)
    {
        if (val.Length != Constants.CurrencyCodeLength || val.Any(x => !char.IsLetter(x)))
        {
            throw new ValidationException($"{paramName?.TrimName()} value '{val}' is not a valid currency code.");
        }
        return val;
    }

    private static string? TrimName(this string? paramName) => paramName?.Split('.').Last();
}
