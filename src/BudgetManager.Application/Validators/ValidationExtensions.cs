using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using BudgetManager.Application.Models;
using BudgetManager.Domain;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Models;
using BudgetManager.Domain.Services;

namespace BudgetManager.Application.Validators;

public static class ValidationExtensions
{
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

    public static decimal EnsureNotGreaterThan(this decimal val, decimal max, [CallerArgumentExpression(nameof(val))] string? paramName = null)
    {
        if (val > max)
        {
            throw new ValidationException($"{paramName?.TrimName()} must be less than or equal {max}.");
        }
        return val;
    }

    public static IEnumerable<CreateFundDTO> EnsureValidFunds(this IEnumerable<CreateFundDTO> funds)
    {
        foreach (var fund in funds)
        {
            if (fund == null)
            {
                throw new ValidationException("Fund cannot be empty.");
            }

            fund.Name.EnsureNotEmpty("Fund name").EnsureNotLongerThan(Constants.MaxNameLength, "Fund name");
        }

        if (funds.GroupBy(x => x.Name).Any(g => g.Count() > 1))
        {
            throw new ValidationException("Fund names in a budget must be unique.");
        }

        return funds;
    }

    /// <summary>
    /// Validates the funds of an edited budget: the rules of a new budget's funds, and every existing fund listed once.
    /// </summary>
    public static IReadOnlyList<UpdateFundDTO> EnsureValidFunds(this IEnumerable<UpdateFundDTO>? funds)
    {
        var array = funds?.ToArray() ?? [];
        if (array.Length == 0)
        {
            throw new ValidationException("Funds cannot be empty.");
        }
        if (array.Any(x => x == null))
        {
            throw new ValidationException("Fund cannot be empty.");
        }

        array.Select(x => new CreateFundDTO(x.Name)).EnsureValidFunds();

        var ids = array.Where(x => x.Id is not null).Select(x => x.Id!.Value).ToArray();
        foreach (var id in ids)
        {
            id.EnsureNotEmpty("Fund");
        }
        if (ids.Distinct().Count() != ids.Length)
        {
            throw new ValidationException("A fund cannot be listed more than once.");
        }

        return array;
    }

    /// <summary>
    /// Validates the allocation templates of a new budget: a name that is unique in the budget, a currency, and steps
    /// that follow the pipeline rules and name funds of <paramref name="funds"/>. A null <paramref name="templates"/> is none.
    /// </summary>
    public static IEnumerable<CreateAllocationTemplateDTO> EnsureValidAllocationTemplates(
        this IEnumerable<CreateAllocationTemplateDTO>? templates,
        IEnumerable<CreateFundDTO> funds)
    {
        var array = templates?.ToArray() ?? [];
        var fundNames = funds.Select(x => x.Name).ToHashSet();

        foreach (var template in array)
        {
            if (template == null)
            {
                throw new ValidationException("Allocation template cannot be empty.");
            }

            template.Name.EnsureNotEmpty("Allocation template name").EnsureNotLongerThan(Constants.MaxNameLength, "Allocation template name");
            template.Currency.EnsureValidCurrency($"Allocation template {template.Name} currency");

            var lines = template.Lines?.ToArray() ?? [];
            if (lines.Any(x => x == null))
            {
                throw new ValidationException("Allocation line cannot be empty.");
            }
            foreach (var line in lines)
            {
                line.Fund.EnsureNotEmpty("Allocation line fund");
                if (!fundNames.Contains(line.Fund))
                {
                    throw new ValidationException($"Allocation template {template.Name} allocates to unknown fund {line.Fund}.");
                }
            }

            EnsureValidSteps([.. lines.Select(x => new Step(x.Sequence, x.Type, x.Amount, x.Percent))], $"Allocation template {template.Name}");
        }

        if (array.GroupBy(x => x.Name).Any(g => g.Count() > 1))
        {
            throw new ValidationException("Allocation template names in a budget must be unique.");
        }

        return array;
    }

    /// <summary>
    /// Validates the allocation of a new ledger's initial balances: every allocation names a fund of <paramref name="budgets"/>,
    /// and in each currency the allocations add up to exactly the positive total of the <paramref name="accounts"/>' initial balances.
    /// A null <paramref name="allocations"/> is treated as none, so positive balances still have to be allocated.
    /// </summary>
    public static IEnumerable<InitialAllocationDTO> EnsureValidInitialAllocations(
        this IEnumerable<InitialAllocationDTO>? allocations,
        IEnumerable<CreateBudgetDTO> budgets,
        IEnumerable<CreateAccountDTO> accounts)
    {
        var array = allocations?.ToArray() ?? [];

        foreach (var allocation in array)
        {
            if (allocation == null)
            {
                throw new ValidationException("Initial allocation cannot be empty.");
            }

            var name = $"Initial allocation to {allocation.Budget} / {allocation.Fund}";
            if (allocation.Value == null)
            {
                throw new ValidationException($"{name} value cannot be empty.");
            }
            if (allocation.Value.Amount <= 0)
            {
                throw new ValidationException($"{name} amount must be positive.");
            }
            allocation.Value.EnsureValidInitialBalance(name);

            if (!budgets.Any(b => b.Name == allocation.Budget && b.Funds.Any(f => f.Name == allocation.Fund)))
            {
                throw new ValidationException($"{name} does not match any fund of the ledger's budgets.");
            }
        }

        if (array.DistinctBy(x => (x.Budget, x.Fund, x.Value.Currency)).Count() != array.Length)
        {
            throw new ValidationException("Initial allocations must have unique funds per currency.");
        }

        var totals = accounts
            .GroupBy(x => x.InitialBalance.Currency)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.InitialBalance.Amount));
        var allocated = array
            .GroupBy(x => x.Value.Currency)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Value.Amount));

        foreach (var currency in totals.Keys.Union(allocated.Keys).Order(StringComparer.Ordinal))
        {
            var total = totals.GetValueOrDefault(currency);
            var sum = allocated.GetValueOrDefault(currency);

            if (total <= 0)
            {
                if (allocated.ContainsKey(currency))
                {
                    throw new ValidationException($"Nothing to allocate in {currency}.");
                }
                continue;
            }
            if (sum != total)
            {
                throw new ValidationException(FormattableString.Invariant($"Initial allocations in {currency} must add up to {total} (got {sum})."));
            }
        }

        return array;
    }

    public static IEnumerable<AllocationLine> EnsureValidAllocationLines(this IEnumerable<AllocationLine> lines, string currency)
    {
        currency.EnsureValidCurrency("Allocation template currency");

        EnsureValidRules(lines?.ToArray() ?? []);

        return lines!;
    }

    /// <summary>
    /// Validates the lines of a ledger's allocation template: no empty line or budget, then the pipeline rules of
    /// <see cref="EnsureValidSteps"/>. Budgets may repeat.
    /// </summary>
    public static IEnumerable<LedgerAllocationLine> EnsureValidLedgerAllocationLines(this IEnumerable<LedgerAllocationLine> lines, string currency)
    {
        currency.EnsureValidCurrency("Allocation template currency");

        var array = lines?.ToArray() ?? [];
        foreach (var line in array)
        {
            if (line == null)
            {
                throw new ValidationException("Allocation line cannot be empty.");
            }
            line.BudgetId.EnsureNotEmpty("Allocation line budget");
        }

        EnsureValidSteps([.. array.Select(x => new Step(x.Sequence, x.Type, x.Amount, x.Percent))], "Allocation line");

        return array;
    }

    /// <summary>
    /// The rules shared by template lines and custom allocation lines: no empty line or fund, then the pipeline rules
    /// of <see cref="EnsureValidSteps"/>. Funds may repeat.
    /// </summary>
    private static void EnsureValidRules(AllocationLine[] lines)
    {
        foreach (var line in lines)
        {
            if (line == null)
            {
                throw new ValidationException("Allocation line cannot be empty.");
            }
            line.FundId.EnsureNotEmpty("Allocation line fund");
        }

        EnsureValidSteps([.. lines.Select(x => new Step(x.Sequence, x.Type, x.Amount, x.Percent))], "Allocation line");
    }

    private readonly record struct Step(int Sequence, AllocationType Type, decimal? Amount, decimal? Percent);

    /// <summary>
    /// The pipeline rules: at least one step, unique sequences, every value valid for its type (a fixed amount above zero,
    /// a percent above zero and up to 100), a 100% step only last because it takes everything that is left, and a minimum
    /// that does not exceed the maximum amount.
    /// </summary>
    private static void EnsureValidSteps(Step[] steps, string name)
    {
        if (steps.Length == 0)
        {
            throw new ValidationException("Allocation lines cannot be empty.");
        }

        if (steps.DistinctBy(x => x.Sequence).Count() != steps.Length)
        {
            throw new ValidationException("Allocation lines must have unique sequences.");
        }

        foreach (var step in steps)
        {
            EnsureValidAllocationValue(step.Sequence, step.Type, step.Amount, step.Percent, name);
        }

        var ordered = steps.OrderBy(x => x.Sequence).ToArray();
        if (ordered[..^1].Any(x => x.Type == AllocationType.Percent && x.Percent == Constants.MaxAllocationPercent))
        {
            throw new ValidationException("A 100% step takes everything that is left, so it must be the last step.");
        }

        var minimum = AllocationCalculator.Minimum(ordered.Select(x => new AllocationRule(
            Guid.Empty,
            x.Sequence,
            x.Type,
            (x.Type == AllocationType.Fixed ? x.Amount : x.Percent) ?? 0)));
        if (minimum > Constants.MaxMoneyAmount)
        {
            throw new ValidationException("The steps need more than the maximum amount to fund every fixed step.");
        }
    }

    /// <summary>
    /// Validates the allocation of <paramref name="income"/> to funds, by a template or by custom lines that follow the
    /// pipeline rules of a template. Whether the income reaches the minimum is checked when it is split. With
    /// <paramref name="allowClear"/> (an update), empty lines and no template are accepted for any amount: they only
    /// remove the existing allocations.
    /// </summary>
    public static AllocationInput EnsureValidAllocation(this AllocationInput allocation, Money income, bool allowClear = false)
    {
        if (allowClear && allocation.Lines is not null && !allocation.AllocatesAnything)
        {
            return allocation;
        }
        if (income.Amount <= 0)
        {
            throw new ValidationException("Only an income can be allocated to funds.");
        }
        if (new object?[] { allocation.TemplateId, allocation.Lines, allocation.LedgerTemplate }.Count(x => x is not null) != 1)
        {
            throw new ValidationException("Allocation must have exactly one of a template, lines or a ledger template.");
        }
        if (allocation.TemplateId is { } templateId)
        {
            templateId.EnsureNotEmpty("Allocation template");
            return allocation;
        }
        if (allocation.LedgerTemplate is { } ledgerTemplate)
        {
            ledgerTemplate.TemplateId.EnsureNotEmpty("Ledger allocation template");
            var budgetTemplateIds = ledgerTemplate.BudgetTemplateIds?.ToArray() ?? [];
            foreach (var budgetTemplateId in budgetTemplateIds)
            {
                budgetTemplateId.EnsureNotEmpty("Budget allocation template");
            }
            if (budgetTemplateIds.Distinct().Count() != budgetTemplateIds.Length)
            {
                throw new ValidationException("Budget allocation templates must be unique.");
            }
            return allocation;
        }

        EnsureValidRules([.. allocation.Lines!.Select(x => x?.ToLine()!)]);

        return allocation;
    }

    /// <summary>
    /// Validates the fund an expense is paid from: nothing to check when there is none, otherwise the value must be an
    /// expense and the transaction cannot also be allocated.
    /// </summary>
    public static void EnsureValidExpenseFund(this Guid? fundId, Money value, AllocationInput? allocation)
    {
        if (fundId is not { } id)
        {
            return;
        }

        id.EnsureNotEmpty("Fund");
        if (value.Amount >= 0)
        {
            throw new ValidationException("Only an expense can be paid from a fund.");
        }
        if (allocation?.AllocatesAnything == true)
        {
            throw new ValidationException("A transaction cannot be both allocated and paid from a fund.");
        }
    }

    public static void EnsureValidRange(this DateTimeOffset? from, DateTimeOffset? to)
    {
        if (from > to)
        {
            throw new ValidationException("From cannot be later than To.");
        }
    }

    private static void EnsureValidAllocationValue(int sequence, AllocationType type, decimal? amount, decimal? percent, string name)
    {
        sequence.EnsureNonnegative($"{name} sequence");

        switch (type)
        {
            case AllocationType.Fixed:
                if (amount == null)
                {
                    throw new ValidationException($"{name} amount is required.");
                }
                if (amount.Value <= 0)
                {
                    throw new ValidationException($"{name} amount must be greater than zero.");
                }
                amount.Value.EnsureValidAmount($"{name} amount");
                if (percent != null)
                {
                    throw new ValidationException($"{name} cannot have a percent.");
                }
                break;

            case AllocationType.Percent:
                if (percent == null)
                {
                    throw new ValidationException($"{name} percent is required.");
                }
                if (percent.Value <= 0)
                {
                    throw new ValidationException($"{name} percent must be greater than zero.");
                }
                percent.Value
                    .EnsureNotGreaterThan(Constants.MaxAllocationPercent, $"{name} percent")
                    .EnsureNotMorePreciseThan(Constants.PercentDecimalPlaces, $"{name} percent");
                if (amount != null)
                {
                    throw new ValidationException($"{name} cannot have an amount.");
                }
                break;

            default:
                throw new ValidationException($"{name} type '{type}' is not valid.");
        }
    }

    public static Guid EnsureNotEmpty(this Guid val, [CallerArgumentExpression(nameof(val))] string? paramName = null)
    {
        if (val == Guid.Empty)
        {
            throw new ValidationException($"{paramName?.TrimName()} cannot be empty.");
        }
        return val;
    }

    public static IEnumerable<T> EnsureNotEmpty<T>(this IEnumerable<T> val, [CallerArgumentExpression(nameof(val))] string? paramName = null)
    {
        if (val == null || !val.Any())
        {
            throw new ValidationException($"{paramName?.TrimName()} cannot be empty.");
        }
        return val;
    }

    public static Money EnsureValid(this Money val, [CallerArgumentExpression(nameof(val))] string? paramName = null)
    {
        if (val.Amount == 0)
        {
            throw new ValidationException($"{paramName?.TrimName()} amount cannot be zero.");
        }
        return val.EnsureValidInitialBalance(paramName?.TrimName());
    }

    public static Money EnsureValidPositive(this Money val, [CallerArgumentExpression(nameof(val))] string? paramName = null)
    {
        if (val.Amount < 0)
        {
            throw new ValidationException($"{paramName?.TrimName()} amount must be positive.");
        }
        return val.EnsureValid(paramName?.TrimName());
    }

    public static Money EnsureValidInitialBalance(this Money val, [CallerArgumentExpression(nameof(val))] string? paramName = null)
    {
        val.Currency.EnsureValidCurrency($"{paramName?.TrimName()} currency");
        val.Amount.EnsureValidAmount($"{paramName?.TrimName()} amount");

        return val;
    }

    public static decimal EnsureValidAmount(this decimal val, [CallerArgumentExpression(nameof(val))] string? paramName = null)
    {
        if (Math.Abs(val) > Constants.MaxMoneyAmount)
        {
            throw new ValidationException($"{paramName?.TrimName()} value '{val}' is too large. Max absolute value is {Constants.MaxMoneyAmount}.");
        }
        return val.EnsureNotMorePreciseThan(Constants.MoneyDecimalPlaces, paramName);
    }

    public static decimal EnsureNotMorePreciseThan(this decimal val, int decimalPlaces, [CallerArgumentExpression(nameof(val))] string? paramName = null)
    {
        if (decimal.Round(val, decimalPlaces) != val)
        {
            throw new ValidationException($"{paramName?.TrimName()} value '{val}' cannot have more than {decimalPlaces} decimal places.");
        }
        return val;
    }

    public static string EnsureValidCurrency(this string val, [CallerArgumentExpression(nameof(val))] string? paramName = null)
    {
        if (val == null || val.Length != Constants.CurrencyCodeLength || val.Any(x => !char.IsAsciiLetterUpper(x)))
        {
            throw new ValidationException($"{paramName?.TrimName()} value '{val}' is not a valid currency code.");
        }
        return val;
    }

    public static string EnsureOneOf(this string val, IReadOnlyList<string> allowed, [CallerArgumentExpression(nameof(val))] string? paramName = null)
    {
        if (val == null || !allowed.Contains(val))
        {
            throw new ValidationException($"{paramName?.TrimName()} value '{val}' is not valid. Allowed values are: {string.Join(", ", allowed)}.");
        }
        return val;
    }

    public static IEnumerable<string> EnsureValidFavoriteCurrencies(this IEnumerable<string>? currencies, [CallerArgumentExpression(nameof(currencies))] string? paramName = null)
    {
        if (currencies == null)
        {
            throw new ValidationException($"{paramName?.TrimName()} cannot be empty.");
        }

        var array = currencies.ToArray();
        if (array.Length > Constants.MaxFavoriteCurrencies)
        {
            throw new ValidationException($"{paramName?.TrimName()} cannot have more than {Constants.MaxFavoriteCurrencies} entries.");
        }
        foreach (var currency in array)
        {
            currency.EnsureValidCurrency($"{paramName?.TrimName()} currency");
        }
        if (array.Distinct().Count() != array.Length)
        {
            throw new ValidationException($"{paramName?.TrimName()} must be unique.");
        }
        return array;
    }

    public static UserPreferences EnsureValidPreferences(this UserPreferencesDTO preferences)
    {
        preferences.Theme.EnsureOneOf(Constants.Themes);
        preferences.DateFormat.EnsureOneOf(Constants.DateFormats);
        preferences.DefaultCurrency.EnsureValidCurrency();
        var favoriteCurrencies = preferences.FavoriteCurrencies.EnsureValidFavoriteCurrencies();
        preferences.Language.EnsureOneOf(Constants.Languages);

        return new UserPreferences
        {
            Theme = preferences.Theme,
            DateFormat = preferences.DateFormat,
            DefaultCurrency = preferences.DefaultCurrency,
            FavoriteCurrencies = [.. favoriteCurrencies],
            Language = preferences.Language
        };
    }

    public static string NormalizeEmail(this string email)
     => email.Trim().ToLowerInvariant();

    public static string EnsureValidEmail(this string val, [CallerArgumentExpression(nameof(val))] string? paramName = null)
    {
        val.EnsureNotEmpty(paramName).EnsureNotLongerThan(Constants.MaxEmailLength, paramName);
        if (!Regex.IsMatch(val, Constants.EmailRegexp))
        {
            throw new ValidationException($"{paramName?.TrimName()} value '{val}' is not a valid email address.");
        }
        return val;
    }

    public static string EnsureValidPassword(this string val, [CallerArgumentExpression(nameof(val))] string? paramName = null)
    {
        if (val == null || val.Length < Constants.MinPasswordLength)
        {
            throw new ValidationException($"{paramName?.TrimName()} must be at least {Constants.MinPasswordLength} characters long.");
        }
        if (Encoding.UTF8.GetByteCount(val) > Constants.MaxPasswordBytes)
        {
            throw new ValidationException($"{paramName?.TrimName()} is too long. Max length is {Constants.MaxPasswordBytes} bytes.");
        }
        return val;
    }

    private static string? TrimName(this string? paramName)
     => paramName == null || paramName.Any(char.IsWhiteSpace) ? paramName : paramName.Split('.').Last();
}
