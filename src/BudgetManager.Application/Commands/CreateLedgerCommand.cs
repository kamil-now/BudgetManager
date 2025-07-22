using BudgetManager.Application.Models;

namespace BudgetManager.Application.Commands;

public record CreateLedgerCommand(
  string Name,
  string? Description,
  CreateBudgetDTO Budget,
  IEnumerable<CreateAccountDTO> Accounts
) : IRequest<Guid>;
