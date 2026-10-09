using System.Linq.Expressions;
using BudgetManager.Application.Models;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Models;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Interfaces;
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
    public void EnsureValidTags_WhenValueIsNull_ShouldReturnNull()
    {
        // Arrange
        string[]? tags = null;

        // Act 
        var result = tags.EnsureValidTags();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void EnsureValidTags_WhenValueIsValid_ShouldReturnInputValue()
    {
        // Arrange
        string[] tags = ["some tag", "another", "12345", "@#!$%^&*()_=+-;':\\/[]{}|<>?,."];

        // Act 
        var result = tags.EnsureValidTags();

        // Assert
        result.ShouldBeEquivalentTo(tags);
    }

    [Fact]
    public void EnsureValidTags_WhenSomeTagIsInvalid_ShouldThrowValidationException()
    {
        // Arrange
        string[] tags = ["some tag", " "];

        // Act & Assert
        var ex = Should.Throw<ValidationException>(() => tags.EnsureValidTags());
        ex.Message.ShouldBeEquivalentTo("Each tag must have a value.");
    }

    [Fact]
    public void EnsureValidTags_WhenCombinedTagsLengthExceedsMax_ShouldThrowValidationException()
    {
        // Arrange
        var longTag = new string('t', Constants.MaxTagsLength - 4 + 1); // -4 to account for 'tag' and a tag separator
        string[] tags = ["tag", longTag];

        // Act & Assert
        var ex = Should.Throw<ValidationException>(() => tags.EnsureValidTags());
        ex.Message.ShouldBeEquivalentTo($"Tags are too long. Max combined tags length is {Constants.MaxTagsLength}.");
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
            const string name = "Allocation template of Fund";
            var fixedAllocation = new FundAllocation("EUR", 0, AllocationType.Fixed, Amount: 100);
            var percentAllocation = new FundAllocation("EUR", 0, AllocationType.Percent, Percent: 10);

            Add([new(string.Empty, [])], "Fund name cannot be empty.");
            Add([new(new string('a', Constants.MaxNameLength + 1), [])], $"Fund name value is too long. Max length is {Constants.MaxNameLength}.");
            Add([new("Fund", [], new string('a', Constants.MaxCommentLength + 1))], $"Description of Fund value is too long. Max length is {Constants.MaxCommentLength}.");
            Add([new("Fund", [null!])], $"{name} cannot be empty.");
            Add([new("Fund", [fixedAllocation with { Currency = "EU" }])], $"{name} currency value 'EU' is not a valid currency code.");
            Add([new("Fund", [fixedAllocation with { Sequence = -1 }])], $"{name} sequence must be greater than or equal zero.");
            Add([new("Fund", [fixedAllocation with { Type = (AllocationType)2 }])], $"{name} type '2' is not valid.");
            Add([new("Fund", [fixedAllocation with { Amount = null }])], $"{name} amount is required.");
            Add([new("Fund", [fixedAllocation with { Amount = -1 }])], $"{name} amount must be greater than or equal zero.");
            Add([new("Fund", [fixedAllocation with { Amount = 1.234m }])], $"{name} amount value '{1.234m}' cannot have more than {Constants.MoneyDecimalPlaces} decimal places.");
            Add([new("Fund", [fixedAllocation with { Amount = Constants.MaxMoneyAmount + 1 }])], $"{name} amount value '{Constants.MaxMoneyAmount + 1}' is too large. Max absolute value is {Constants.MaxMoneyAmount}.");
            Add([new("Fund", [fixedAllocation with { Percent = 10 }])], $"{name} cannot have a percent.");
            Add([new("Fund", [percentAllocation with { Percent = null }])], $"{name} percent is required.");
            Add([new("Fund", [percentAllocation with { Percent = -1 }])], $"{name} percent must be greater than or equal zero.");
            Add([new("Fund", [percentAllocation with { Percent = 100.01m }])], $"{name} percent must be less than or equal {Constants.MaxAllocationPercent}.");
            Add([new("Fund", [percentAllocation with { Percent = 1.234m }])], $"{name} percent value '{1.234m}' cannot have more than {Constants.PercentDecimalPlaces} decimal places.");
            Add([new("Fund", [percentAllocation with { Amount = 100 }])], $"{name} cannot have an amount.");
            Add([new("Fund", [fixedAllocation, percentAllocation with { Sequence = 1 }])], "Allocation templates of Fund must have unique currencies.");
            Add([new("Fund A", [percentAllocation with { Percent = 60 }]), new("Fund B", [percentAllocation with { Sequence = 1, Percent = 40.01m }])], $"Allocation percents in EUR must add up to less than or equal {Constants.MaxAllocationPercent}.");
            Add([new("Fund A", [fixedAllocation]), new("Fund B", [percentAllocation])], "Allocation sequences in EUR must be unique.");
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
            Add([new("Fund", [])]);
            Add([new("Fund", [new("EUR", 0, AllocationType.Fixed, Amount: 0)])]);
            Add([new("Fund A", [new("EUR", 0, AllocationType.Percent, Percent: 60)]), new("Fund B", [new("EUR", 1, AllocationType.Percent, Percent: 40)])]);
            Add([new("Fund A", [new("EUR", 0, AllocationType.Fixed, Amount: 100)]), new("Fund B", [new("USD", 0, AllocationType.Fixed, Amount: 100)])]);
            Add([new("Fund", [new("EUR", 0, AllocationType.Percent, Percent: 100), new("USD", 0, AllocationType.Percent, Percent: 100)])]);
        }
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
}
