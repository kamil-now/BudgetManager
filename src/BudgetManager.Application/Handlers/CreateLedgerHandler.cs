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
        var budget = new Budget
        {
            LedgerId = ledgerId,
            Name = command.Budget.Name,
            Description = command.Budget.Description
        };
        foreach (var fund in command.Budget.Funds)
        {
            budget.AddFund(fund.Name, fund.Description, fund.AllocationTemplates);
        }

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
                    Transactions = [new AccountTransaction()
                    {
                        Title = Constants.InitialBalanceTransactionTitle,
                        Value = x.InitialBalance,
                        AccountId = accountId
                    }]
                };
            })],
            Budgets = [budget]
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
        command.Description?.EnsureNotLongerThan(Constants.MaxCommentLength);
        command.Accounts.EnsureNotEmpty();

        foreach (var account in command.Accounts)
        {
            account.Name.EnsureNotEmpty("Account name").EnsureNotLongerThan(Constants.MaxNameLength, "Account name");
            account.Description?.EnsureNotLongerThan(Constants.MaxCommentLength, $"Description of {account.Name}");
            account.InitialBalance.EnsureValidInitialBalance($"InitialBalance of {account.Name}");
        }

        command.Budget.Name.EnsureNotEmpty("Budget name").EnsureNotLongerThan(Constants.MaxNameLength, "Budget name");
        command.Budget.Description?.EnsureNotLongerThan(Constants.MaxCommentLength, $"Description of {command.Budget.Name}");
        command.Budget.Funds.EnsureNotEmpty().EnsureValidFunds();
    }
}
