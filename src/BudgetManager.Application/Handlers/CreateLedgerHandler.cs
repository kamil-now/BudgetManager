using BudgetManager.Application.Commands;
using BudgetManager.Application.Services;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;

namespace BudgetManager.Application.Handlers;

public sealed class CreateLedgerHandler(ICurrentUserService currentUser, IEntityStore store) : IRequestHandler<CreateLedgerCommand, Guid>
{
    public async Task<Guid> Handle(CreateLedgerCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        Validate(command);

        var ledgerId = Guid.NewGuid();
        var budgets = command.Budgets.Select(b =>
        {
            var budget = new Budget
            {
                LedgerId = ledgerId,
                Name = b.Name,
                Description = b.Description
            };
            foreach (var fund in b.Funds)
            {
                budget.AddFund(fund.Name, fund.Description, fund.AllocationTemplates);
            }
            return budget;
        }).ToList();

        var entity = await store.CreateAsync(new Ledger
        {
            Id = ledgerId,
            OwnerId = userId,
            Name = command.Name,
            Description = command.Description,
            Accounts = [.. command.Accounts.Select(x =>
            {
                var accountId = Guid.NewGuid();
                return new Account()
                {
                    Id = accountId,
                    LedgerId = ledgerId,
                    Name = x.Name,
                    Description = x.Description,
                    Transactions = [AccountTransaction.CreateInitialBalance(accountId, x.InitialBalance)]
                };
            })],
            Budgets = budgets
        }, cancellationToken) ?? throw new InvalidOperationException("Failed to create ledger.");

        await store.SaveChangesAsync(cancellationToken);

        if (entity.Id == Guid.Empty)
        {
            throw new InvalidOperationException("Ledger ID is empty.");
        }
        return entity.Id;
    }

    private static void Validate(CreateLedgerCommand command)
    {
        command.Name.EnsureNotEmpty("Ledger name").EnsureNotLongerThan(Constants.MaxNameLength, "Ledger name");
        command.Description?.EnsureNotLongerThan(Constants.MaxCommentLength);
        command.Accounts.EnsureNotEmpty();

        if (command.Accounts.Any(x => x == null))
        {
            throw new ValidationException("Account cannot be empty.");
        }

        foreach (var account in command.Accounts)
        {
            account.Name.EnsureNotEmpty("Account name").EnsureNotLongerThan(Constants.MaxNameLength, "Account name");
            account.Description?.EnsureNotLongerThan(Constants.MaxCommentLength, $"Description of {account.Name}");
            account.InitialBalance.EnsureValidInitialBalance($"InitialBalance of {account.Name}");
        }

        if (command.Accounts.GroupBy(x => x.Name).Any(g => g.Count() > 1))
        {
            throw new ValidationException("Account names must be unique.");
        }

        command.Budgets.EnsureNotEmpty();

        if (command.Budgets.Any(x => x == null))
        {
            throw new ValidationException("Budget cannot be empty.");
        }

        foreach (var budget in command.Budgets)
        {
            budget.Name.EnsureNotEmpty("Budget name").EnsureNotLongerThan(Constants.MaxNameLength, "Budget name");
            budget.Description?.EnsureNotLongerThan(Constants.MaxCommentLength, $"Description of {budget.Name}");
            budget.Funds.EnsureNotEmpty().EnsureValidFunds();
        }

        if (command.Budgets.GroupBy(x => x.Name).Any(g => g.Count() > 1))
        {
            throw new ValidationException("Budget names must be unique.");
        }
    }
}
