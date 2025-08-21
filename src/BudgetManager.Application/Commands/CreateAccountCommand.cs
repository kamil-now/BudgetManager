using BudgetManager.Common.Models;

namespace BudgetManager.Application.Commands;

public record CreateAccountCommand(
  Guid OwnerId,
  Guid? LedgerId,
  Money InitialBalance,
  string Name,
  string? Description = null
) : IRequest<Guid>;
