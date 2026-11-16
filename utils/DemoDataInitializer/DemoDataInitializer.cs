using BudgetManager.Application.Commands;
using BudgetManager.Application.Models;
using BudgetManager.Application.Queries;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Models;
using BudgetManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BudgetManager.DemoDataInitializer;

/// <summary>
/// Creates the demo user (<see cref="Email"/> / <see cref="Password"/>) with a "Personal" and a "Business" ledger, three budgets in each,
/// and about five months of EUR history ending today: monthly incomes split by allocation templates (the latest one left unallocated),
/// recurring and occasional expenses paid from their funds, transfers and one-off purchases.
/// Everything is done with the application's commands as the demo user, in one database transaction;
/// nothing is created when the user exists already.
/// </summary>
public sealed class DemoDataInitializer(IMediator mediator, DemoUser user, ApplicationDbContext dbContext)
{
    public const string Email = "test@example.com";
    public const string Password = "testtest";
    public const string UserName = "Test User";

    private const string Eur = "EUR";
    private const int HistoryDays = 150;
    private const int Months = 5;

    private readonly DateTimeOffset today = new(DateTimeOffset.UtcNow.UtcDateTime.Date, TimeSpan.Zero);
    private readonly Random random = new(42);

    /// <returns>false when the user exists already and nothing was created.</returns>
    public async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        UserDTO created;
        try
        {
            created = await mediator.Send(new CreateUserCommand(Email, Password, UserName), cancellationToken);
        }
        catch (ConflictException)
        {
            return false;
        }

        user.SignIn(created.Id);

        await CreatePersonalLedgerAsync(cancellationToken);
        await CreateBusinessLedgerAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task CreatePersonalLedgerAsync(CancellationToken ct)
    {
        var start = today.AddDays(-HistoryDays);

        var ledgerId = await mediator.Send(new CreateLedgerCommand(
            "Personal",
            [
                new CreateBudgetDTO("Household", [new("Rent"), new("Utilities"), new("Groceries"), new("Transport"), new("Health")]),
                new CreateBudgetDTO("Lifestyle", [new("Dining out"), new("Entertainment"), new("Clothing"), new("Subscriptions")]),
                new CreateBudgetDTO("Goals", [new("Emergency fund"), new("Vacation"), new("New laptop")])
            ],
            [
                new(new(3400, Eur), "Checking"),
                new(new(12000, Eur), "Savings"),
                new(new(150, Eur), "Cash")
            ],
            [
                new("Household", "Rent", new(1250, Eur)),
                new("Household", "Utilities", new(180, Eur)),
                new("Household", "Groceries", new(400, Eur)),
                new("Household", "Transport", new(120, Eur)),
                new("Household", "Health", new(150, Eur)),
                new("Lifestyle", "Dining out", new(150, Eur)),
                new("Lifestyle", "Entertainment", new(80, Eur)),
                new("Lifestyle", "Clothing", new(200, Eur)),
                new("Lifestyle", "Subscriptions", new(60, Eur)),
                new("Goals", "Emergency fund", new(10960, Eur)),
                new("Goals", "Vacation", new(1200, Eur)),
                new("Goals", "New laptop", new(800, Eur))
            ]), ct);

        var ledger = await mediator.Send(new GetLedgerQuery(ledgerId), ct);
        var accounts = ledger.Accounts.ToDictionary(x => x.Name, x => x.Id);
        var household = ledger.Budgets.Single(x => x.Name == "Household");
        var lifestyle = ledger.Budgets.Single(x => x.Name == "Lifestyle");
        var goals = ledger.Budgets.Single(x => x.Name == "Goals");
        var funds = ledger.Budgets.SelectMany(x => x.Funds).ToDictionary(x => x.Name, x => x.Id);

        var householdTemplate = await CreateTemplateAsync(household.Id, "Salary: household", funds,
            [("Rent", AllocationType.Fixed, 1250), ("Utilities", AllocationType.Fixed, 180), ("Groceries", AllocationType.Fixed, 450), ("Transport", AllocationType.Fixed, 120), ("Health", AllocationType.Fixed, 60)], ct);
        var lifestyleTemplate = await CreateTemplateAsync(lifestyle.Id, "Salary: lifestyle", funds,
            [("Dining out", AllocationType.Fixed, 220), ("Entertainment", AllocationType.Fixed, 90), ("Clothing", AllocationType.Fixed, 120), ("Subscriptions", AllocationType.Fixed, 60)], ct);
        var goalsTemplate = await CreateTemplateAsync(goals.Id, "Salary: goals", funds,
            [("Vacation", AllocationType.Fixed, 250), ("New laptop", AllocationType.Fixed, 150), ("Emergency fund", AllocationType.Percent, 100)], ct);

        var checking = accounts["Checking"];
        var cash = accounts["Cash"];

        // incomes: the latest salary and the freelance job are left for the user to allocate
        for (var month = 0; month < Months; month++)
        {
            var payday = start.AddDays(5 + 30 * month);
            var allocated = month < Months - 1;

            await mediator.Send(new CreateAccountTransactionCommand(
                checking, new(3600, Eur), payday, "Salary", allocated ? new AllocationInput(TemplateId: householdTemplate) : null), ct);

            if (allocated)
            {
                await mediator.Send(new AllocateFundsCommand(ledgerId, Eur, new AllocationInput(TemplateId: lifestyleTemplate), Amount: 490, Date: payday), ct);
                await mediator.Send(new AllocateFundsCommand(ledgerId, Eur, new AllocationInput(TemplateId: goalsTemplate), Amount: 1050, Date: payday), ct);
            }

            await mediator.Send(new CreateAccountTransferCommand(checking, accounts["Savings"], new(400, Eur), payday.AddDays(2), "Monthly savings"), ct);
            await mediator.Send(new CreateAccountTransferCommand(checking, cash, new(100, Eur), payday.AddDays(3), "Cash withdrawal"), ct);
        }
        await mediator.Send(new CreateAccountTransactionCommand(checking, new(450, Eur), start.AddDays(70), "Freelance website project"), ct);

        for (var month = 0; month < Months; month++)
        {
            var payday = start.AddDays(5 + 30 * month);
            var firstOfMonth = payday.AddDays(1);

            await Spend(checking, funds["Rent"], 1250, firstOfMonth, "Rent", ct);
            await Spend(checking, funds["Utilities"], Amount(150, 210), payday.AddDays(8), "Electricity and internet", ct);
            await Spend(checking, funds["Transport"], 49, firstOfMonth, "Public transport pass", ct);
            await Spend(checking, funds["Subscriptions"], 13.99m, payday.AddDays(4), "Netflix", ct);
            await Spend(checking, funds["Subscriptions"], 10.99m, payday.AddDays(4), "Spotify", ct);
            await Spend(checking, funds["Subscriptions"], 29.90m, payday.AddDays(6), "Gym membership", ct);
            await Spend(checking, funds["Health"], Amount(15, 40), payday.AddDays(12), "Pharmacy", ct);
            await Spend(checking, funds["Entertainment"], Amount(12, 35), payday.AddDays(15), "Cinema", ct);
            if (month % 2 == 0)
            {
                await Spend(checking, funds["Clothing"], Amount(60, 110), payday.AddDays(18), "Clothes", ct);
            }
        }

        for (var day = start.AddDays(2); day <= today; day = day.AddDays(7))
        {
            await Spend(checking, funds["Groceries"], Amount(55, 115), day, day.Day % 2 == 0 ? "Supermarket" : "Weekly groceries", ct);
            if (day.AddDays(3) <= today)
            {
                await Spend(cash, funds["Dining out"], Amount(12, 30), day.AddDays(3), random.Next(2) == 0 ? "Lunch" : "Coffee and cake", ct);
            }
        }

        await Spend(checking, funds["New laptop"], 1099, start.AddDays(95), "Laptop", ct);
        await Spend(checking, funds["Vacation"], 1350, start.AddDays(100), "Flights and hotel", ct);

        await mediator.Send(new CreateAccountTransferCommand(accounts["Savings"], checking, new(300, Eur), today.AddDays(-4), "Car insurance top-up"), ct);
    }

    private async Task CreateBusinessLedgerAsync(CancellationToken ct)
    {
        var start = today.AddDays(-HistoryDays);

        var ledgerId = await mediator.Send(new CreateLedgerCommand(
            "Business",
            [
                new CreateBudgetDTO("Operations", [new("Software"), new("Office"), new("Accounting")]),
                new CreateBudgetDTO("Taxes", [new("VAT"), new("Income tax")]),
                new CreateBudgetDTO("Growth", [new("Marketing"), new("Training"), new("Reserve")])
            ],
            [
                new(new(8000, Eur), "Business account"),
                new(new(6000, Eur), "Tax account")
            ],
            [
                new("Operations", "Software", new(400, Eur)),
                new("Operations", "Office", new(300, Eur)),
                new("Operations", "Accounting", new(300, Eur)),
                new("Taxes", "VAT", new(3200, Eur)),
                new("Taxes", "Income tax", new(2800, Eur)),
                new("Growth", "Marketing", new(600, Eur)),
                new("Growth", "Training", new(400, Eur)),
                new("Growth", "Reserve", new(6000, Eur))
            ]), ct);

        var ledger = await mediator.Send(new GetLedgerQuery(ledgerId), ct);
        var accounts = ledger.Accounts.ToDictionary(x => x.Name, x => x.Id);
        var operations = ledger.Budgets.Single(x => x.Name == "Operations");
        var taxes = ledger.Budgets.Single(x => x.Name == "Taxes");
        var growth = ledger.Budgets.Single(x => x.Name == "Growth");
        var funds = ledger.Budgets.SelectMany(x => x.Funds).ToDictionary(x => x.Name, x => x.Id);

        var operationsTemplate = await CreateTemplateAsync(operations.Id, "Invoice: operations", funds,
            [("Software", AllocationType.Fixed, 180), ("Office", AllocationType.Fixed, 120), ("Accounting", AllocationType.Fixed, 150)], ct);
        var taxesTemplate = await CreateTemplateAsync(taxes.Id, "Invoice: taxes", funds,
            [("VAT", AllocationType.Fixed, 800), ("Income tax", AllocationType.Fixed, 900)], ct);
        var growthTemplate = await CreateTemplateAsync(growth.Id, "Invoice: growth", funds,
            [("Marketing", AllocationType.Fixed, 300), ("Training", AllocationType.Fixed, 150), ("Reserve", AllocationType.Percent, 100)], ct);

        var business = accounts["Business account"];
        var taxAccount = accounts["Tax account"];

        // one invoice is left for the user to allocate
        for (var month = 0; month < Months; month++)
        {
            var date = start.AddDays(10 + 30 * month);
            var allocated = month < Months - 1;

            await mediator.Send(new CreateAccountTransactionCommand(
                business, new(5000, Eur), date, $"Invoice #{2031 + month}", allocated ? new AllocationInput(TemplateId: operationsTemplate) : null), ct);

            if (allocated)
            {
                await mediator.Send(new AllocateFundsCommand(ledgerId, Eur, new AllocationInput(TemplateId: taxesTemplate), Amount: 1700, Date: date), ct);
                await mediator.Send(new AllocateFundsCommand(ledgerId, Eur, new AllocationInput(TemplateId: growthTemplate), Amount: 2850, Date: date), ct);
            }

            await mediator.Send(new CreateAccountTransferCommand(business, taxAccount, new(1700, Eur), date.AddDays(1), "Tax reserve"), ct);
        }

        for (var month = 0; month < Months; month++)
        {
            var date = start.AddDays(10 + 30 * month);

            await Spend(business, funds["Software"], Amount(38, 62), date.AddDays(3), "Cloud hosting", ct);
            await Spend(business, funds["Software"], 29, date.AddDays(3), "Invoicing software", ct);
            await Spend(business, funds["Office"], 99, date.AddDays(2), "Coworking space", ct);
            await Spend(business, funds["Accounting"], 150, date.AddDays(5), "Accountant", ct);
            await Spend(business, funds["Marketing"], Amount(120, 250), date.AddDays(7), "Online ads", ct);
            if (month % 2 == 1)
            {
                await Spend(business, funds["Training"], Amount(49, 120), date.AddDays(9), "Online course", ct);
            }
        }

        await Spend(taxAccount, funds["VAT"], 2400, start.AddDays(75), "Quarterly VAT payment", ct);
        await Spend(taxAccount, funds["Income tax"], 2700, start.AddDays(95), "Income tax advance", ct);
        await Spend(business, funds["Training"], 450, start.AddDays(110), "Conference ticket", ct);
    }

    private async Task<Guid> CreateTemplateAsync(
        Guid budgetId,
        string name,
        IReadOnlyDictionary<string, Guid> funds,
        IEnumerable<(string Fund, AllocationType Type, decimal Value)> steps,
        CancellationToken ct)
        => await mediator.Send(new CreateAllocationTemplateCommand(
            budgetId,
            name,
            Eur,
            steps.Select((x, i) => new AllocationLine(
                funds[x.Fund],
                i,
                x.Type,
                Amount: x.Type == AllocationType.Fixed ? x.Value : null,
                Percent: x.Type == AllocationType.Percent ? x.Value : null))
                .ToList()), ct);

    private async Task Spend(Guid accountId, Guid fundId, decimal amount, DateTimeOffset date, string title, CancellationToken ct)
    {
        if (date <= today)
        {
            await mediator.Send(new CreateAccountTransactionCommand(accountId, new(-amount, Eur), date, title, FundId: fundId), ct);
        }
    }

    private decimal Amount(int min, int max) => random.Next(min * 100, max * 100) / 100m;
}
