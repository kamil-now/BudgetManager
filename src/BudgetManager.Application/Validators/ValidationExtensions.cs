using System.Runtime.CompilerServices;
using BudgetManager.Application.Services;
using BudgetManager.Common.Models;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Validators;

public static class ValidationExtensions
{
    public static async Task<Guid> EnsureExistsAsync(this ICurrentUserService currentUser, IBudgetManagerService service, CancellationToken cancellationToken)
    {
        var userId = currentUser.Id.EnsureValidId();

        await userId.EnsureExists<User>(service, cancellationToken);

        return userId;
    }

    public static Guid EnsureValidId(this string? val, [CallerArgumentExpression(nameof(val))] string? paramName = null)
    {
        if (val is null || !Guid.TryParse(val, out var userId) || userId == Guid.Empty)
        {
            throw new ValidationException($"{paramName?.TrimName()} value '{val}' is invalid.");
        }
        return userId;
    }

    public static IEnumerable<string>? EnsureValidTags(this IEnumerable<string>? tags)
    {
        if (tags != null)
        {
            if (tags.Any(tag => string.IsNullOrWhiteSpace(tag)))
            {
                throw new ValidationException("Tags cannot contain null or whitespace values.");
            }
            if (string.Join(',', tags).Length > Constants.MaxTagsLength)
            {
                throw new ValidationException($"Tags value is too long. Max combined tags length is {Constants.MaxTagsLength}.");
            }
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

    public static IEnumerable<T> EnsureNotEmpty<T>(this IEnumerable<T> val, [CallerArgumentExpression(nameof(val))] string? paramName = null)
    {
        if (!val.Any())
        {
            throw new ValidationException($"{paramName?.TrimName()} cannot be empty.");
        }
        return val;
    }

    public static Guid EnsureNotEmpty(this Guid val)
    {
        if (val == Guid.Empty)
        {
            throw new ValidationException($"ID cannot be empty.");
        }
        return val;
    }

    public static async Task EnsureExists<T>(this Guid id, IBudgetManagerService service, CancellationToken cancellationToken) where T : Entity
    {
        if (id == Guid.Empty)
        {
            throw new ValidationException($"Entity ID cannot be empty.");
        }
        if (!await service.ExistsAsync<T>(x => x.Id == id, cancellationToken))
        {
            throw new ValidationException($"Entity with ID '{id}' does not exist.");
        }
    }

    public static string EnsureValidCurrency(this string val, [CallerArgumentExpression(nameof(val))] string? paramName = null)
    {
        if (val.Length != Constants.CurrencyCodeLength || val.Any(x => !char.IsLetter(x)))
        {
            throw new ValidationException($"{paramName?.TrimName()} value '{val}' is not a valid currency code.");
        }
        return val;
    }

    public static Money EnsureValid(this Money val, [CallerArgumentExpression(nameof(val))] string? paramName = null)
    {
        val.Currency.EnsureValidCurrency($"{paramName?.TrimName()} currency");
        val.Amount.EnsureNonnegative($"{paramName?.TrimName()} amount");

        return val;
    }

    private static string? TrimName(this string? paramName) => paramName?.Split('.').Last();
}
