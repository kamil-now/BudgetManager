using System.Runtime.CompilerServices;
using BudgetManager.Application.Services;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Validators;

public static class ValidationExtensions
{
  public static async Task<Guid> EnsureExistsAsync(this ICurrentUserService currentUser, IBudgetManagerService service, CancellationToken cancellationToken)
  {
    var userId = currentUser.Id.EnsureValid();

    await service.EnsureExists<User>(userId, cancellationToken);

    return userId;
  }

  public static Guid EnsureValid(this string? val, [CallerArgumentExpression(nameof(val))] string? paramName = null)
  {
    if (val is null || !Guid.TryParse(val, out var userId) || userId == Guid.Empty)
    {
      throw new ValidationException($"{paramName} value '{val}' is invalid.");
    }
    return userId;
  }

  public static IEnumerable<T> EnsureNotLongerThan<T>(this IEnumerable<T> val, int max, [CallerArgumentExpression(nameof(val))] string? paramName = null)
  {
    if (val.Count() > max)
    {
      throw new ValidationException($"{paramName} value is too long. Max length is {max}.");
    }
    return val;
  }

  public static int EnsureNonnegative(this int val, [CallerArgumentExpression(nameof(val))] string? paramName = null)
   => (int)EnsureNonnegative((decimal)val, paramName);

  public static decimal EnsureNonnegative(this decimal val, [CallerArgumentExpression(nameof(val))] string? paramName = null)
  {
    if (val < 0)
    {
      throw new ValidationException($"{paramName} must be greater than or equal zero.");
    }
    return val;
  }

  public static IEnumerable<T> EnsureNotEmpty<T>(this IEnumerable<T> val, [CallerArgumentExpression(nameof(val))] string? paramName = null)
  {
    if (!val.Any())
    {
      throw new ValidationException($"{paramName} cannot be empty.");
    }
    return val;
  }

  public static async Task EnsureExists<T>(this IBudgetManagerService service, Guid id, CancellationToken cancellationToken) where T : Entity
  {
    if (!await service.ExistsAsync<T>(x => x.Id == id, cancellationToken))
    {
      throw new ValidationException($"Entity with ID '{id}' does not exist.");
    }
  }

  public static string EnsureValidCurrency(this string val, [CallerArgumentExpression(nameof(val))] string? paramName = null)
  {
    if (val.Length != 3 || val.Any(x => !char.IsLetter(x)))
    {
      throw new ValidationException($"{paramName} value '{val}' is not a valid currency code.");
    }
    return val;
  }
}
