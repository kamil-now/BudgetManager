using System.Runtime.CompilerServices;
using BudgetManager.Application.Services;
using BudgetManager.Common.Models;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Validators;

public static class ValidationExtensions
{
    public static async Task<Guid> EnsureAccessibleAsync<T>(this Guid id, ICurrentUserService currentUser, IBudgetManagerService service, CancellationToken cancellationToken) where T : Entity, IAccessControlled
    {
        var ownerId = await service.GetOwnerIdAsync<T>(id, cancellationToken)
            ?? throw new ValidationException($"Entity with ID '{id}' does not exist.");

        if (ownerId.ToString() != currentUser.Id)
        {
            throw new AccessException($"{typeof(T).Name} with ID '{id}' cannot be accessed by user with ID '{currentUser.Id}'.");
        }
        return id;
    }

    public static async Task<Guid> EnsureExistsAsync(this ICurrentUserService currentUser, IBudgetManagerService service, CancellationToken cancellationToken)
    {
        var userId = currentUser.Id.EnsureValidId();

        await userId.EnsureExistsAsync<User>(service, cancellationToken);

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

    public static async Task<Guid> EnsureExistsAsync<T>(this Guid id, IBudgetManagerService service, CancellationToken cancellationToken) where T : Entity
    {
        if (id == Guid.Empty)
        {
            throw new ValidationException($"{typeof(T).Name} ID cannot be empty.");
        }
        if (!await service.ExistsAsync<T>(x => x.Id == id, cancellationToken))
        {
            throw new ValidationException($"{typeof(T).Name} with ID '{id}' does not exist.");
        }
        return id;
    }

    public static Money EnsureValid(this Money val, [CallerArgumentExpression(nameof(val))] string? paramName = null)
    {
        val.Amount.EnsurePositive($"{paramName?.TrimName()} amount");
        val.Currency.EnsureValidCurrency($"{paramName?.TrimName()} currency");

        return val;
    }

    private static decimal EnsurePositive(this decimal val, [CallerArgumentExpression(nameof(val))] string? paramName = null)
    {
        if (val <= 0)
        {
            throw new ValidationException($"{paramName?.TrimName()} must be greater than zero.");
        }
        return val;
    }

    private static string EnsureValidCurrency(this string val, [CallerArgumentExpression(nameof(val))] string? paramName = null)
    {
        if (val.Length != Constants.CurrencyCodeLength || val.Any(x => !char.IsLetter(x)))
        {
            throw new ValidationException($"{paramName?.TrimName()} value '{val}' is not a valid currency code.");
        }
        return val;
    }

    private static string? TrimName(this string? paramName) => paramName?.Split('.').Last();
}
