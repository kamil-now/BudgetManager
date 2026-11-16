using BudgetManager.Application.Security;

namespace BudgetManager.Application.Commands;

/// <summary>Replaces all preferences of the current user.</summary>
public record UpdateUserPreferencesCommand(
  string Theme,
  string DateFormat,
  string DefaultCurrency,
  IEnumerable<string> FavoriteCurrencies,
  string Language
) : IRequest, IRequiresAccess
{
    IEnumerable<Resource> IRequiresAccess.Resources => [];
}
