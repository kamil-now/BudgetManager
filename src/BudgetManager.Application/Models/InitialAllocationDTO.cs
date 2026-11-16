using BudgetManager.Domain.Models;

namespace BudgetManager.Application.Models;

/// <summary>
/// The part of the new ledger's initial balances that goes to the fund named <see cref="Fund"/> of the budget named <see cref="Budget"/>.
/// </summary>
public record InitialAllocationDTO(string Budget, string Fund, Money Value);
