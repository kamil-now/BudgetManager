using System.Runtime.CompilerServices;
using BudgetManager.Application.Models;
using BudgetManager.Domain.Models;
using BudgetManager.Domain;
using BudgetManager.Domain.Enums;

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

    public static decimal EnsureNotGreaterThan(this decimal val, decimal max, [CallerArgumentExpression(nameof(val))] string? paramName = null)
    {
        if (val > max)
        {
            throw new ValidationException($"{paramName?.TrimName()} must be less than or equal {max}.");
        }
        return val;
    }

    public static IEnumerable<CreateFundDTO> EnsureValidFunds(this IEnumerable<CreateFundDTO> funds)
    {
        foreach (var fund in funds)
        {
            fund.Name.EnsureNotEmpty("Fund name").EnsureNotLongerThan(Constants.MaxNameLength, "Fund name");
            fund.Description?.EnsureNotLongerThan(Constants.MaxCommentLength, $"Description of {fund.Name}");

            var templates = fund.AllocationTemplates.ToArray();
            var name = $"Allocation template of {fund.Name}";

            foreach (var template in templates)
            {
                if (template == null)
                {
                    throw new ValidationException($"{name} cannot be empty.");
                }
                template.Currency.EnsureValidCurrency($"{name} currency");
                template.Sequence.EnsureNonnegative($"{name} sequence");

                switch (template.Type)
                {
                    case AllocationType.Fixed:
                        if (template.Amount == null)
                        {
                            throw new ValidationException($"{name} amount is required.");
                        }
                        template.Amount.Value
                            .EnsureNonnegative($"{name} amount")
                            .EnsureValidAmount($"{name} amount");
                        if (template.Percent != null)
                        {
                            throw new ValidationException($"{name} cannot have a percent.");
                        }
                        break;

                    case AllocationType.Percent:
                        if (template.Percent == null)
                        {
                            throw new ValidationException($"{name} percent is required.");
                        }
                        template.Percent.Value
                            .EnsureNonnegative($"{name} percent")
                            .EnsureNotGreaterThan(Constants.MaxAllocationPercent, $"{name} percent")
                            .EnsureNotMorePreciseThan(Constants.PercentDecimalPlaces, $"{name} percent");
                        if (template.Amount != null)
                        {
                            throw new ValidationException($"{name} cannot have an amount.");
                        }
                        break;

                    default:
                        throw new ValidationException($"{name} type '{template.Type}' is not valid.");
                }
            }

            if (templates.DistinctBy(x => x.Currency).Count() != templates.Length)
            {
                throw new ValidationException($"Allocation templates of {fund.Name} must have unique currencies.");
            }
        }

        foreach (var currency in funds.SelectMany(x => x.AllocationTemplates).GroupBy(x => x.Currency))
        {
            if (currency.Sum(x => x.Percent ?? 0) > Constants.MaxAllocationPercent)
            {
                throw new ValidationException($"Allocation percents in {currency.Key} must add up to less than or equal {Constants.MaxAllocationPercent}.");
            }
            if (currency.DistinctBy(x => x.Sequence).Count() != currency.Count())
            {
                throw new ValidationException($"Allocation sequences in {currency.Key} must be unique.");
            }
        }

        return funds;
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
        return val.EnsureValidInitialBalance(paramName?.TrimName());
    }

    public static Money EnsureValidInitialBalance(this Money val, [CallerArgumentExpression(nameof(val))] string? paramName = null)
    {
        val.Currency.EnsureValidCurrency($"{paramName?.TrimName()} currency");
        val.Amount.EnsureValidAmount($"{paramName?.TrimName()} amount");

        return val;
    }

    public static decimal EnsureValidAmount(this decimal val, [CallerArgumentExpression(nameof(val))] string? paramName = null)
    {
        if (Math.Abs(val) > Constants.MaxMoneyAmount)
        {
            throw new ValidationException($"{paramName?.TrimName()} value '{val}' is too large. Max absolute value is {Constants.MaxMoneyAmount}.");
        }
        return val.EnsureNotMorePreciseThan(Constants.MoneyDecimalPlaces, paramName);
    }

    public static decimal EnsureNotMorePreciseThan(this decimal val, int decimalPlaces, [CallerArgumentExpression(nameof(val))] string? paramName = null)
    {
        if (decimal.Round(val, decimalPlaces) != val)
        {
            throw new ValidationException($"{paramName?.TrimName()} value '{val}' cannot have more than {decimalPlaces} decimal places.");
        }
        return val;
    }

    public static string EnsureValidCurrency(this string val, [CallerArgumentExpression(nameof(val))] string? paramName = null)
    {
        if (val == null || val.Length != Constants.CurrencyCodeLength || val.Any(x => !char.IsAsciiLetterUpper(x)))
        {
            throw new ValidationException($"{paramName?.TrimName()} value '{val}' is not a valid currency code.");
        }
        return val;
    }

    private static string? TrimName(this string? paramName)
     => paramName == null || paramName.Any(char.IsWhiteSpace) ? paramName : paramName.Split('.').Last();
}
