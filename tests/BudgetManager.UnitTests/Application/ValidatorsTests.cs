using System.Linq.Expressions;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Models;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
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
    [InlineData(0.01, "usd")]
    [InlineData(1, "eur")]
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
}
