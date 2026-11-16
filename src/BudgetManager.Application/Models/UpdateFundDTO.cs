namespace BudgetManager.Application.Models;

/// <summary>
/// A fund of an edited budget: an existing fund when <paramref name="Id"/> is set, a new one otherwise.
/// </summary>
public record UpdateFundDTO(Guid? Id, string Name);
