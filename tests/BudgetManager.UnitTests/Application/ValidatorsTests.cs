using System.Linq.Expressions;
using BudgetManager.Application.Models;
using BudgetManager.Application.Validators;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Interfaces;
using BudgetManager.Domain.Models;
using NSubstitute;
using Shouldly;

namespace BudgetManager.UnitTests.Application;

public class ValidatorsTests
{
    [Fact]
    public void EnsureNotEmpty_WhenIdIsEmptyGuid_ShouldThrowValidationException()
    {
        // Arrange
        var ledgerId = Guid.Empty;

        // Act & Assert
        var ex = Should.Throw<ValidationException>(() => ledgerId.EnsureNotEmpty());
        ex.Message.ShouldBeEquivalentTo("ledgerId cannot be empty.");
    }

    [Fact]
    public void EnsureNotEmpty_WhenIdIsSet_ShouldReturnIt()
    {
        // Arrange
        var ledgerId = Guid.NewGuid();

        // Act
        var result = ledgerId.EnsureNotEmpty();

        // Assert
        result.ShouldBe(ledgerId);
    }

    [Fact]
    public void EnsureNotLongerThan_WhenCountExceedsMax_ShouldThrowValidationException()
    {
        // Arrange
        var max = 1;
        var input = (string[])["1", "2"];

        // Act & Assert
        var ex = Should.Throw<ValidationException>(() => input.EnsureNotLongerThan(max));
        ex.Message.ShouldBeEquivalentTo($"input value is too long. Max length is {max}.");
    }

    [Fact]
    public void EnsureNotLongerThan_WhenCountDoesNotExceedMax_ShouldReturnInput()
    {
        // Arrange
        var max = 2;
        var input = (string[])["1", "2"];

        // Act 
        var result = input.EnsureNotLongerThan(max);

        // Assert
        result.ShouldBeEquivalentTo(input);
    }

    [Fact]
    public void EnsureNonnegative_WhenValueIsNegative_ShouldThrowValidationException()
    {
        // Arrange
        var input = -1;

        // Act & Assert
        var ex = Should.Throw<ValidationException>(() => input.EnsureNonnegative("ParamName"));
        ex.Message.ShouldBeEquivalentTo("ParamName must be greater than or equal zero.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void EnsureNonnegative_WhenValueIsNonNegative_ShouldReturnInput(int input)
    {
        // Act 
        var result = input.EnsureNonnegative();

        // Assert
        result.ShouldBeEquivalentTo(input);
    }

    [Fact]
    public void EnsureNotEmpty_WhenCollectionIsEmpty_ShouldThrowValidationException()
    {
        // Arrange
        var input = Array.Empty<int>();

        // Act & Assert
        var ex = Should.Throw<ValidationException>(() => input.EnsureNotEmpty());
        ex.Message.ShouldBeEquivalentTo("input cannot be empty.");
    }

    [Fact]
    public void EnsureNotEmpty_WhenCollectionIsNotEmpty_ShouldReturnInput()
    {
        // Arrange
        var input = (string[])["1"];

        // Act 
        var result = input.EnsureNotEmpty();

        // Assert
        result.ShouldBeEquivalentTo(input);
    }

    [Theory]
    [InlineData(0.000000000000000001, "US", "input currency value 'US' is not a valid currency code.")]
    [InlineData(1, "usds", "input currency value 'usds' is not a valid currency code.")]
    [InlineData(1, "usd", "input currency value 'usd' is not a valid currency code.")]
    [InlineData(1, "Eur", "input currency value 'Eur' is not a valid currency code.")]
    [InlineData(0, "u", "input amount cannot be zero.")]
    [InlineData(-1, "?", "input currency value '?' is not a valid currency code.")]
    [InlineData(1.234, "USD", "input amount value '1.234' cannot have more than 2 decimal places.")]
    public void EnsureValid_WhenMoneyIsInvalid_ShouldThrowValidationException(decimal amount, string currency, string expectedException)
    {
        // Arrange
        var input = new Money(amount, currency);

        // Act & Assert
        var ex = Should.Throw<ValidationException>(() => input.EnsureValid());
        ex.Message.ShouldBeEquivalentTo(expectedException);
    }

    [Theory]
    [InlineData(123456789.12, "USD")]
    [InlineData(0.01, "USD")]
    [InlineData(1, "EUR")]
    [InlineData(1, "XCD")]
    public void EnsureValid_WhenMoneyIsValid_ShouldReturnInput(decimal amount, string currency)
    {
        // Arrange
        var input = new Money(amount, currency);

        // Act 
        var result = input.EnsureValid();

        // Assert
        result.ShouldBeEquivalentTo(input);
    }

    [Fact]
    public void EnsureValidAmount_WhenValueIsTooLarge_ShouldThrowValidationException()
    {
        // Arrange
        var input = -Constants.MaxMoneyAmount - 1;

        // Act & Assert
        var ex = Should.Throw<ValidationException>(() => input.EnsureValidAmount());
        ex.Message.ShouldBeEquivalentTo($"input value '{input}' is too large. Max absolute value is {Constants.MaxMoneyAmount}.");
    }

    [Fact]
    public void EnsureValidCurrency_WhenValueIsNull_ShouldThrowValidationException()
    {
        // Arrange
        string input = null!;

        // Act & Assert
        var ex = Should.Throw<ValidationException>(() => input.EnsureValidCurrency());
        ex.Message.ShouldBeEquivalentTo("input value '' is not a valid currency code.");
    }

    [Fact]
    public void EnsureNotGreaterThan_WhenValueIsGreaterThanMax_ShouldThrowValidationException()
    {
        // Arrange
        var input = 100.01m;

        // Act & Assert
        var ex = Should.Throw<ValidationException>(() => input.EnsureNotGreaterThan(100));
        ex.Message.ShouldBeEquivalentTo("input must be less than or equal 100.");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100)]
    public void EnsureNotGreaterThan_WhenValueIsNotGreaterThanMax_ShouldReturnInput(decimal input)
    {
        // Act
        var result = input.EnsureNotGreaterThan(100);

        // Assert
        result.ShouldBe(input);
    }

    [Fact]
    public void EnsureNotMorePreciseThan_WhenValueHasTooManyDecimalPlaces_ShouldThrowValidationException()
    {
        // Arrange
        var input = 1.001m;

        // Act & Assert
        var ex = Should.Throw<ValidationException>(() => input.EnsureNotMorePreciseThan(2));
        ex.Message.ShouldBeEquivalentTo($"input value '{input}' cannot have more than 2 decimal places.");
    }

    public sealed class InvalidFunds : TheoryData<CreateFundDTO[], string>
    {
        public InvalidFunds()
        {
            Add([new(string.Empty)], "Fund name cannot be empty.");
            Add([new(new string('a', Constants.MaxNameLength + 1))], $"Fund name value is too long. Max length is {Constants.MaxNameLength}.");
            Add([null!], "Fund cannot be empty.");
            Add([new("Fund"), new("Fund")], "Fund names in a budget must be unique.");
        }
    }

    [Theory]
    [ClassData(typeof(InvalidFunds))]
    public void EnsureValidFunds_WhenFundsAreInvalid_ShouldThrowValidationException(CreateFundDTO[] funds, string expectedException)
    {
        // Act & Assert
        var ex = Should.Throw<ValidationException>(() => funds.EnsureValidFunds());
        ex.Message.ShouldBeEquivalentTo(expectedException);
    }

    public sealed class ValidFunds : TheoryData<CreateFundDTO[]>
    {
        public ValidFunds()
        {
            Add([new("Fund")]);
            Add([new("Fund A"), new("Fund B")]);
        }
    }

    public sealed class InvalidAllocationTemplates : TheoryData<CreateAllocationTemplateDTO[], string>
    {
        public InvalidAllocationTemplates()
        {
            const string name = "Allocation template Salary";
            var fixedLine = new CreateAllocationLineDTO("Food", 0, AllocationType.Fixed, Amount: 100);
            var percentLine = new CreateAllocationLineDTO("Food", 0, AllocationType.Percent, Percent: 10);
            var template = new CreateAllocationTemplateDTO("Salary", "EUR", [fixedLine]);

            Add([null!], "Allocation template cannot be empty.");
            Add([template with { Name = string.Empty }], "Allocation template name cannot be empty.");
            Add([template with { Name = new string('a', Constants.MaxNameLength + 1) }], $"Allocation template name value is too long. Max length is {Constants.MaxNameLength}.");
            Add([template with { Currency = "EU" }], $"{name} currency value 'EU' is not a valid currency code.");
            Add([template, template with { Currency = "USD" }], "Allocation template names in a budget must be unique.");
            Add([template with { Lines = [] }], "Allocation lines cannot be empty.");
            Add([template with { Lines = [null!] }], "Allocation line cannot be empty.");
            Add([template with { Lines = [fixedLine with { Fund = "Rent" }] }], $"{name} allocates to unknown fund Rent.");
            Add([template with { Lines = [fixedLine, percentLine] }], "Allocation lines must have unique sequences.");
            Add([template with { Lines = [fixedLine with { Sequence = -1 }] }], $"{name} sequence must be greater than or equal zero.");
            Add([template with { Lines = [fixedLine with { Amount = 0 }] }], $"{name} amount must be greater than zero.");
            Add([template with { Lines = [percentLine with { Percent = 0 }] }], $"{name} percent must be greater than zero.");
            Add([template with { Lines = [percentLine with { Percent = 100.01m }] }], $"{name} percent must be less than or equal {Constants.MaxAllocationPercent}.");
            Add([template with { Lines = [percentLine with { Percent = 100 }, fixedLine with { Sequence = 1 }] }], "A 100% step takes everything that is left, so it must be the last step.");
            Add([template with { Lines = [percentLine with { Percent = 100 }, percentLine with { Percent = 100, Sequence = 1 }] }], "A 100% step takes everything that is left, so it must be the last step.");
            Add([template with { Lines = [percentLine with { Percent = 50 }, fixedLine with { Sequence = 1, Amount = Constants.MaxMoneyAmount }] }], "The steps need more than the maximum amount to fund every fixed step.");
        }
    }

    [Theory]
    [ClassData(typeof(InvalidAllocationTemplates))]
    public void EnsureValidAllocationTemplates_WhenTemplatesAreInvalid_ShouldThrowValidationException(CreateAllocationTemplateDTO[] templates, string expectedException)
    {
        // Arrange
        CreateFundDTO[] funds = [new("Food")];

        // Act & Assert
        var ex = Should.Throw<ValidationException>(() => templates.EnsureValidAllocationTemplates(funds));
        ex.Message.ShouldBeEquivalentTo(expectedException);
    }

    public sealed class ValidAllocationTemplates : TheoryData<CreateAllocationTemplateDTO[]?>
    {
        public ValidAllocationTemplates()
        {
            Add(null);
            Add([]);
            Add([new("Salary", "EUR", [new("Food", 0, AllocationType.Fixed, Amount: 100)])]);
            Add([new("Salary", "EUR", [new("Food", 0, AllocationType.Percent, Percent: 100)])]);
            Add([new("Salary", "EUR", [new("Food", 0, AllocationType.Percent, Percent: 80), new("Food", 1, AllocationType.Percent, Percent: 80)])]);
            Add([new("Salary", "EUR", [new("Food", 0, AllocationType.Fixed, Amount: 100), new("Food", 3, AllocationType.Percent, Percent: 100)])]);
            Add([new("Salary", "EUR", [new("Food", 0, AllocationType.Percent, Percent: 10)]), new("Bonus", "EUR", [new("Food", 0, AllocationType.Percent, Percent: 10)])]);
        }
    }

    [Theory]
    [ClassData(typeof(ValidAllocationTemplates))]
    public void EnsureValidAllocationTemplates_WhenTemplatesAreValid_ShouldNotThrow(CreateAllocationTemplateDTO[]? templates)
    {
        // Arrange
        CreateFundDTO[] funds = [new("Food")];

        // Act & Assert
        Should.NotThrow(() => templates.EnsureValidAllocationTemplates(funds));
    }

    [Theory]
    [ClassData(typeof(ValidFunds))]
    public void EnsureValidFunds_WhenFundsAreValid_ShouldReturnInput(CreateFundDTO[] funds)
    {
        // Act
        var result = funds.EnsureValidFunds();

        // Assert
        result.ShouldBeSameAs(funds);
    }

    private static readonly CreateBudgetDTO[] InitialBudgets =
    [
        new("Household", [new("Food"), new("Rent")]),
        new("Travel", [new("Flights"), new("Food")]),
    ];

    public sealed class InvalidInitialAllocations : TheoryData<InitialAllocationDTO[]?, CreateAccountDTO[], string>
    {
        public InvalidInitialAllocations()
        {
            const string name = "Initial allocation to Household / Food";
            CreateAccountDTO[] eur100 = [new(new(100, "EUR"), "Cash")];
            var food = new InitialAllocationDTO("Household", "Food", new(100, "EUR"));

            Add(null, eur100, "Initial allocations in EUR must add up to 100 (got 0).");
            Add([], eur100, "Initial allocations in EUR must add up to 100 (got 0).");
            Add([null!], eur100, "Initial allocation cannot be empty.");
            Add([food with { Value = null! }], eur100, $"{name} value cannot be empty.");
            Add([food with { Value = new(0, "EUR") }], eur100, $"{name} amount must be positive.");
            Add([food with { Value = new(-1, "EUR") }], eur100, $"{name} amount must be positive.");
            Add([food with { Value = new(1.234m, "EUR") }], eur100, $"{name} amount value '{1.234m}' cannot have more than {Constants.MoneyDecimalPlaces} decimal places.");
            Add([food with { Value = new(100, "EU") }], eur100, $"{name} currency value 'EU' is not a valid currency code.");
            Add([food with { Fund = "Flights" }], eur100, "Initial allocation to Household / Flights does not match any fund of the ledger's budgets.");
            Add([food with { Budget = "Missing" }], eur100, "Initial allocation to Missing / Food does not match any fund of the ledger's budgets.");
            Add([food with { Value = new(50, "EUR") }, food with { Value = new(50, "EUR") }], eur100, "Initial allocations must have unique funds per currency.");
            Add([food, food with { Value = new(10, "USD") }], eur100, "Nothing to allocate in USD.");
            Add([food with { Value = new(10, "EUR") }], [new(new(0, "EUR"), "Cash")], "Nothing to allocate in EUR.");
            Add([food with { Value = new(10, "EUR") }], [new(new(-100, "EUR"), "Cash")], "Nothing to allocate in EUR.");
            Add([food with { Value = new(60, "EUR") }], eur100, "Initial allocations in EUR must add up to 100 (got 60).");
            Add([food with { Value = new(60, "EUR") }, food with { Fund = "Rent", Value = new(50, "EUR") }], eur100, "Initial allocations in EUR must add up to 100 (got 110).");
            Add([food], [new(new(100, "EUR"), "Cash"), new(new(50, "USD"), "Dollars")], "Initial allocations in USD must add up to 50 (got 0).");
        }
    }

    [Theory]
    [ClassData(typeof(InvalidInitialAllocations))]
    public void EnsureValidInitialAllocations_WhenAllocationsAreInvalid_ShouldThrowValidationException(InitialAllocationDTO[]? allocations, CreateAccountDTO[] accounts, string expectedException)
    {
        // Act & Assert
        var ex = Should.Throw<ValidationException>(() => allocations.EnsureValidInitialAllocations(InitialBudgets, accounts));
        ex.Message.ShouldBeEquivalentTo(expectedException);
    }

    public sealed class ValidInitialAllocations : TheoryData<InitialAllocationDTO[]?, CreateAccountDTO[]>
    {
        public ValidInitialAllocations()
        {
            Add(null, [new(new(0, "EUR"), "Cash")]);
            Add([], [new(new(-100, "EUR"), "Cash")]);
            Add([new("Household", "Food", new(100, "EUR"))], [new(new(100, "EUR"), "Cash")]);
            Add([new("Household", "Food", new(60, "EUR")), new("Travel", "Food", new(40, "EUR"))], [new(new(100, "EUR"), "Cash")]);
            Add([new("Household", "Food", new(60, "EUR"))], [new(new(100, "EUR"), "Cash"), new(new(-40, "EUR"), "Card")]);
            Add([new("Household", "Food", new(100, "EUR")), new("Household", "Food", new(50, "USD"))], [new(new(100, "EUR"), "Cash"), new(new(50, "USD"), "Dollars")]);
        }
    }

    [Theory]
    [ClassData(typeof(ValidInitialAllocations))]
    public void EnsureValidInitialAllocations_WhenAllocationsAreValid_ShouldNotThrow(InitialAllocationDTO[]? allocations, CreateAccountDTO[] accounts)
    {
        // Act & Assert
        Should.NotThrow(() => allocations.EnsureValidInitialAllocations(InitialBudgets, accounts));
    }

    private static readonly Guid FundA = Guid.Parse("a1a1a1a1-0000-0000-0000-000000000001");
    private static readonly Guid FundB = Guid.Parse("b2b2b2b2-0000-0000-0000-000000000002");

    public sealed class InvalidAllocationLines : TheoryData<AllocationLine[], string, string>
    {
        public InvalidAllocationLines()
        {
            var fixedLine = new AllocationLine(FundA, 0, AllocationType.Fixed, Amount: 100);
            var percentLine = new AllocationLine(FundB, 1, AllocationType.Percent, Percent: 10);
            const string name = "Allocation line";
            var hundred = percentLine with { Percent = 100 };

            Add([fixedLine], "EU", "Allocation template currency value 'EU' is not a valid currency code.");
            Add([], "EUR", "Allocation lines cannot be empty.");
            Add([null!], "EUR", "Allocation line cannot be empty.");
            Add([fixedLine with { FundId = Guid.Empty }], "EUR", "Allocation line fund cannot be empty.");
            Add([fixedLine, percentLine with { Sequence = 0 }], "EUR", "Allocation lines must have unique sequences.");
            Add([fixedLine with { Sequence = -1 }], "EUR", $"{name} sequence must be greater than or equal zero.");
            Add([fixedLine with { Amount = null }], "EUR", $"{name} amount is required.");
            Add([fixedLine with { Amount = -1 }], "EUR", $"{name} amount must be greater than zero.");
            Add([fixedLine with { Amount = 0 }], "EUR", $"{name} amount must be greater than zero.");
            Add([fixedLine with { Percent = 10 }], "EUR", $"{name} cannot have a percent.");
            Add([percentLine with { Percent = 100.01m }], "EUR", $"{name} percent must be less than or equal {Constants.MaxAllocationPercent}.");
            Add([percentLine with { Percent = 0 }], "EUR", $"{name} percent must be greater than zero.");
            Add([percentLine with { Amount = 10 }], "EUR", $"{name} cannot have an amount.");
            Add([fixedLine with { Type = (AllocationType)2 }], "EUR", $"{name} type '2' is not valid.");
            Add([hundred with { Sequence = 0 }, fixedLine with { Sequence = 1 }], "EUR", "A 100% step takes everything that is left, so it must be the last step.");
            Add([percentLine with { Percent = 50, Sequence = 0 }, fixedLine with { Sequence = 1, Amount = Constants.MaxMoneyAmount }], "EUR", "The steps need more than the maximum amount to fund every fixed step.");
        }
    }

    [Theory]
    [ClassData(typeof(InvalidAllocationLines))]
    public void EnsureValidAllocationLines_WhenLinesAreInvalid_ShouldThrowValidationException(AllocationLine[] lines, string currency, string expectedException)
    {
        // Act & Assert
        var ex = Should.Throw<ValidationException>(() => lines.EnsureValidAllocationLines(currency));
        ex.Message.ShouldBeEquivalentTo(expectedException);
    }

    public sealed class ValidAllocationLines : TheoryData<AllocationLine[]>
    {
        public ValidAllocationLines()
        {
            Add([new(FundA, 0, AllocationType.Fixed, Amount: 100)]);
            Add([new(FundA, 3, AllocationType.Percent, Percent: 60), new(FundB, 1, AllocationType.Percent, Percent: 60)]);
            Add([new(FundA, 0, AllocationType.Fixed, Amount: 100), new(FundB, 1, AllocationType.Percent, Percent: 100)]);
            Add([new(FundA, 0, AllocationType.Fixed, Amount: Constants.MaxMoneyAmount)]);
            Add([new(FundA, 0, AllocationType.Fixed, Amount: 100), new(FundA, 1, AllocationType.Percent, Percent: 50)]);
        }
    }

    [Theory]
    [ClassData(typeof(ValidAllocationLines))]
    public void EnsureValidAllocationLines_WhenLinesAreValid_ShouldReturnInput(AllocationLine[] lines)
    {
        // Act
        var result = lines.EnsureValidAllocationLines("EUR");

        // Assert
        result.ShouldBeSameAs(lines);
    }

    public sealed class InvalidAllocations : TheoryData<AllocationInput, decimal, bool, string>
    {
        public InvalidAllocations()
        {
            var template = new AllocationInput(TemplateId: Guid.NewGuid());
            var line = new AllocationLineInput(FundA, 0, AllocationType.Fixed, Amount: 40);
            var percentLine = new AllocationLineInput(FundB, 1, AllocationType.Percent, Percent: 10);
            const string name = "Allocation line";

            Add(template, -100, false, "Only an income can be allocated to funds.");
            Add(template, -100, true, "Only an income can be allocated to funds.");
            const string exactlyOne = "Allocation must have exactly one of a template, lines or a ledger template.";
            var ledgerTemplate = new LedgerAllocationInput(Guid.NewGuid(), [Guid.NewGuid()]);

            Add(new AllocationInput(), 100, false, exactlyOne);
            Add(template with { Lines = [line] }, 100, false, exactlyOne);
            Add(template with { LedgerTemplate = ledgerTemplate }, 100, false, exactlyOne);
            Add(new AllocationInput(Lines: [line], LedgerTemplate: ledgerTemplate), 100, false, exactlyOne);
            Add(new AllocationInput(LedgerTemplate: ledgerTemplate), -100, false, "Only an income can be allocated to funds.");
            Add(new AllocationInput(LedgerTemplate: ledgerTemplate with { TemplateId = Guid.Empty }), 100, false, "Ledger allocation template cannot be empty.");
            Add(new AllocationInput(LedgerTemplate: ledgerTemplate with { BudgetTemplateIds = [Guid.Empty] }), 100, false, "Budget allocation template cannot be empty.");
            Add(new AllocationInput(LedgerTemplate: ledgerTemplate with { BudgetTemplateIds = [FundA, FundA] }), 100, false, "Budget allocation templates must be unique.");
            Add(new AllocationInput(TemplateId: Guid.Empty), 100, false, "Allocation template cannot be empty.");
            Add(new AllocationInput(Lines: []), 100, false, "Allocation lines cannot be empty.");
            Add(new AllocationInput(Lines: [null!]), 100, false, "Allocation line cannot be empty.");
            Add(new AllocationInput(Lines: [line with { FundId = Guid.Empty }]), 100, false, "Allocation line fund cannot be empty.");
            Add(new AllocationInput(Lines: [line with { Amount = null }]), 100, false, $"{name} amount is required.");
            Add(new AllocationInput(Lines: [line with { Amount = -1 }]), 100, false, $"{name} amount must be greater than zero.");
            Add(new AllocationInput(Lines: [line with { Amount = 0 }]), 100, false, $"{name} amount must be greater than zero.");
            Add(new AllocationInput(Lines: [percentLine with { Percent = 0 }]), 100, false, $"{name} percent must be greater than zero.");
            Add(new AllocationInput(Lines: [line with { Amount = 1.234m }]), 100, false, $"{name} amount value '{1.234m}' cannot have more than {Constants.MoneyDecimalPlaces} decimal places.");
            Add(new AllocationInput(Lines: [line with { Percent = 10 }]), 100, false, $"{name} cannot have a percent.");
            Add(new AllocationInput(Lines: [percentLine with { Percent = null }]), 100, false, $"{name} percent is required.");
            Add(new AllocationInput(Lines: [percentLine with { Percent = 100.01m }]), 100, false, $"{name} percent must be less than or equal {Constants.MaxAllocationPercent}.");
            Add(new AllocationInput(Lines: [percentLine with { Percent = 1.234m }]), 100, false, $"{name} percent value '{1.234m}' cannot have more than {Constants.PercentDecimalPlaces} decimal places.");
            Add(new AllocationInput(Lines: [line, percentLine with { Sequence = 0 }]), 100, false, "Allocation lines must have unique sequences.");
            Add(new AllocationInput(Lines: [percentLine with { Sequence = 0, Percent = 100 }, line with { Sequence = 1 }]), 100, false, "A 100% step takes everything that is left, so it must be the last step.");
        }
    }

    [Theory]
    [ClassData(typeof(InvalidAllocations))]
    public void EnsureValidAllocation_WhenAllocationIsInvalid_ShouldThrowValidationException(AllocationInput allocation, decimal income, bool allowClear, string expectedException)
    {
        // Act & Assert
        var ex = Should.Throw<ValidationException>(() => allocation.EnsureValidAllocation(new Money(income, "EUR"), allowClear));
        ex.Message.ShouldBeEquivalentTo(expectedException);
    }

    public sealed class ValidAllocations : TheoryData<AllocationInput, decimal, bool>
    {
        public ValidAllocations()
        {
            Add(new AllocationInput(TemplateId: Guid.NewGuid()), 100, false);
            Add(new AllocationInput(Lines: [new(FundA, 0, AllocationType.Fixed, Amount: 40), new(FundB, 1, AllocationType.Percent, Percent: 60)]), 100, false);
            Add(new AllocationInput(Lines: [new(FundA, 0, AllocationType.Fixed, Amount: 0.01m)]), 100, false);
            Add(new AllocationInput(Lines: [new(FundA, 0, AllocationType.Fixed, Amount: 500)]), 100, false);
            Add(new AllocationInput(Lines: [new(FundA, 0, AllocationType.Fixed, Amount: 10), new(FundA, 1, AllocationType.Percent, Percent: 50)]), 100, false);
            Add(new AllocationInput(Lines: []), 100, true);
            Add(new AllocationInput(Lines: []), -100, true);
            Add(new AllocationInput(LedgerTemplate: new(Guid.NewGuid(), [Guid.NewGuid(), Guid.NewGuid()])), 100, false);
        }
    }

    public sealed class InvalidLedgerAllocationLines : TheoryData<LedgerAllocationLine[], string, string>
    {
        public InvalidLedgerAllocationLines()
        {
            var fixedLine = new LedgerAllocationLine(FundA, 0, AllocationType.Fixed, Amount: 100);
            var percentLine = new LedgerAllocationLine(FundB, 1, AllocationType.Percent, Percent: 10);

            Add([fixedLine], "EU", "Allocation template currency value 'EU' is not a valid currency code.");
            Add([], "EUR", "Allocation lines cannot be empty.");
            Add([null!], "EUR", "Allocation line cannot be empty.");
            Add([fixedLine with { BudgetId = Guid.Empty }], "EUR", "Allocation line budget cannot be empty.");
            Add([fixedLine, percentLine with { Sequence = 0 }], "EUR", "Allocation lines must have unique sequences.");
            Add([fixedLine with { Amount = null }], "EUR", "Allocation line amount is required.");
            Add([percentLine with { Percent = 0 }], "EUR", "Allocation line percent must be greater than zero.");
            Add([percentLine with { Percent = 100, Sequence = 0 }, fixedLine with { Sequence = 1 }], "EUR", "A 100% step takes everything that is left, so it must be the last step.");
        }
    }

    [Theory]
    [ClassData(typeof(InvalidLedgerAllocationLines))]
    public void EnsureValidLedgerAllocationLines_WhenLinesAreInvalid_ShouldThrowValidationException(LedgerAllocationLine[] lines, string currency, string expectedException)
    {
        // Act & Assert
        var ex = Should.Throw<ValidationException>(() => lines.EnsureValidLedgerAllocationLines(currency));
        ex.Message.ShouldBeEquivalentTo(expectedException);
    }

    [Fact]
    public void EnsureValidLedgerAllocationLines_WhenLinesAreValid_ShouldReturnThem()
    {
        // Arrange
        LedgerAllocationLine[] lines =
        [
            new(FundA, 0, AllocationType.Fixed, Amount: 100),
            new(FundB, 1, AllocationType.Percent, Percent: 60),
            new(FundA, 2, AllocationType.Percent, Percent: 100)
        ];

        // Act
        var result = lines.EnsureValidLedgerAllocationLines("EUR");

        // Assert
        result.ShouldBe(lines);
    }

    [Theory]
    [ClassData(typeof(ValidAllocations))]
    public void EnsureValidAllocation_WhenAllocationIsValid_ShouldReturnInput(AllocationInput allocation, decimal income, bool allowClear)
    {
        // Act
        var result = allocation.EnsureValidAllocation(new Money(income, "EUR"), allowClear);

        // Assert
        result.ShouldBeSameAs(allocation);
    }

    [Fact]
    public void EnsureValidRange_WhenFromIsLaterThanTo_ShouldThrowValidationException()
    {
        // Arrange
        DateTimeOffset? from = DateTimeOffset.UtcNow;
        DateTimeOffset? to = from.Value.AddDays(-1);

        // Act & Assert
        var ex = Should.Throw<ValidationException>(() => from.EnsureValidRange(to));
        ex.Message.ShouldBeEquivalentTo("From cannot be later than To.");
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void EnsureValidRange_WhenFromIsNotLaterThanToOrEitherIsMissing_ShouldNotThrow(bool hasFrom, bool hasTo)
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        DateTimeOffset? from = hasFrom ? now : null;
        DateTimeOffset? to = hasTo ? now : null;

        // Act & Assert
        Should.NotThrow(() => from.EnsureValidRange(to));
    }
}
